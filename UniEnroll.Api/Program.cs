using AutoMapper;
using Dapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SendGrid;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Text;
using System.Threading.Channels;
using UniEnroll.Api.Auth;
using UniEnroll.Api.Caching;
using UniEnroll.Application.Common;
using UniEnroll.Application.Common.Idempotency;
using UniEnroll.Api.Mapping;
using UniEnroll.Api.RateLimiting;
using UniEnroll.Api.Realtime;
using UniEnroll.Api.Versioning;
using UniEnroll.Application.Caching;
using UniEnroll.Application.Errors;
using UniEnroll.Application.Handlers.Commands;
using UniEnroll.Application.Security;
using UniEnroll.Domain.Common;
using UniEnroll.Infrastructure;
using UniEnroll.Infrastructure.Observability;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Infrastructure.Transactions;
using UniEnroll.Messaging.Abstractions;
using UniEnroll.Messaging.RabbitMQ;
using UniEnroll.Messaging.SendGrid;
using UniEnroll.Application;

var builder = WebApplication.CreateBuilder(args);

// Serilog first for early pipeline logs
builder.Host.UseSerilog((ctx, sp, cfg) => SerilogSetup.Configure(cfg, ctx.Configuration));

var config = builder.Configuration;

// Core services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails(); // RFC7807
builder.Services.AddHttpContextAccessor();

// AuthN/Z
var jwtOpts = config.GetSection("Jwt").Get<JwtOptions>() ?? new();
builder.Services.AddSingleton(jwtOpts);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(opt =>
  {
      opt.RequireHttpsMetadata = false;
      opt.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwtOpts.Issuer,
          ValidAudience = jwtOpts.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Key))
      };
  });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy(Policies.CapacityOverride, p => p.RequireRole(Roles.Admin, Roles.Registrar));
    opt.AddPolicy(Policies.PrereqWaiver, p => p.RequireRole(Roles.Admin, Roles.Registrar));
});

// Dapper defaults
DefaultTypeMap.MatchNamesWithUnderscores = true;
DapperTypeHandlers.RegisterAll(); // <- add this line early

// Configure Mapping profile so we can inject and use mapper interface directly on controller level
builder.Services.AddSingleton(sp => new MapperConfiguration(config =>
{
    config.AddProfile<ApiProfiles>();
}, sp.GetRequiredService<ILoggerFactory>()).CreateMapper());


// DB factory (per-request scope)
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

// Repositories
builder.Services.AddScoped<ITermsRepository, TermsRepository>();
builder.Services.AddScoped<ICoursesRepository, CoursesRepository>();
builder.Services.AddScoped<IOfferingsRepository, OfferingsRepository>();
builder.Services.AddScoped<IEnrollmentsRepository, EnrollmentsRepository>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();
builder.Services.AddScoped<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddSingleton<IEmailOutboxRepository, EmailOutboxRepository>();

// MediatR + Validators + Mapping
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

// MediatR validation pipeline
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueryCacheBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

// 1) Options + accessor
builder.Services.Configure<IdempotencyOptions>(config.GetSection("Idempotency"));
builder.Services.AddSingleton<IIdempotencyKeyAccessor, HttpIdempotencyKeyAccessor>();

// Ambient DB session (scoped per request)
builder.Services.AddScoped<IDbSession, DbSession>();

//builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));

// Observability
builder.Services.AddOpenTelemetry()
   .WithTracing(t => OpenTelemetrySetup.ConfigureTracing(t))
   .WithMetrics(m => OpenTelemetrySetup.ConfigureMetrics(m));

// after builder creation
builder.Services.AddProblemDetailsCustomization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // .NET 8 IExceptionHandler

// Rate limiting
builder.Services.AddRateLimiter(opt => RateLimitConfig.Configure(opt, config));

// MVC (controllers)
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

// API versioning
builder.Services.AddApiVersioningV1();

// Add policies that require our custom requirements
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.CapacityOverride, p =>
        p.Requirements.Add(new CapacityOverrideRequirement()));
    o.AddPolicy(Policies.PrereqWaiver, p =>
        p.Requirements.Add(new PrereqWaiverRequirement()));
});

// Register handlers (they check Admin/Registrar roles)
builder.Services.AddSingleton<IAuthorizationHandler, CapacityOverrideHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PrereqWaiverHandler>();

// SignalR configuration
builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
    o.MaximumReceiveMessageSize = 64 * 1024;
});

// Whitelist all cross domain urls here, unregistered Domain will be not allowed to use this API
var corsOrigins = config.GetSection("CORS:Origins").Get<string[]>()?.ToArray();
if (corsOrigins?.Length > 0)
{
    builder.Services.AddCors(cors =>
    {
        cors.AddPolicy("corspolicy", (builder) =>
        {
            builder.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()    //Allow SignalR endpoint to negotiate
            .WithExposedHeaders("Content-Disposition", "Content-Type");
        });
    });
}

// Optional distributed cache (Redis): set ConnectionStrings:Redis to enable
var redisCs = config.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisCs))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));
    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.ConnectionMultiplexerFactory = async () => await ConnectionMultiplexer.ConnectAsync(redisCs);
        o.InstanceName = "unienroll:";
    });
}
else
{
    builder.Services.AddMemoryCache();
}

// Email messaging Queue using RabbitMQ + SendGrid
builder.Services.Configure<RabbitMqOptions>(config.GetSection("RabbitMq"));
builder.Services.Configure<SendGridOptions>(config.GetSection("SendGrid"));

// Register SendGrid client
builder.Services.AddSingleton(sp =>
{
    var apiKey = sp.GetRequiredService<IOptions<SendGridOptions>>().Value.ApiKey;
    return new SendGridClient(apiKey);
});
builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();

// Queue backend selection (unchanged)
var useRabbit = !string.IsNullOrWhiteSpace(config["RabbitMq:HostName"]);
if (useRabbit)
{
    builder.Services.AddSingleton<IEmailQueue, RabbitMqEmailQueue>();
}
else
{
    // In-memory queue + worker for local dev
    var channel = Channel.CreateUnbounded<EmailMessage>();
    builder.Services.AddSingleton(channel);
    builder.Services.AddSingleton<IEmailQueue, InMemoryEmailQueue>();
    builder.Services.AddSingleton<IEmailSender, DebugEmailSender>(); // swap with SMTP/SendGrid in prod
    builder.Services.AddHostedService<EmailChannelWorker>(); // this uses IEmailSender (SendGrid) now
}

// Outbox + dispatcher 
builder.Services.AddHostedService<RabbitConsumer>();
builder.Services.AddHostedService<OutboxDispatcher>();


// Output cache
// Register all policies from a single place
builder.Services.AddOutputCachingWithPolicies(config);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "University API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
    foreach (var desc in provider.ApiVersionDescriptions)
        c.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", $"University API {desc.GroupName}");
});

app.UseCors("corspolicy");

// map hub
app.MapHub<EnrollmentHub>("/hubs/enrollments")
   .RequireAuthorization(); // hub requires JWT like your API

//Serilog
app.UseSerilogRequestLogging(opts =>
{
    // JOIN request log with our properties
    opts.EnrichDiagnosticContext = (diag, ctx) =>
    {
        diag.Set("CorrelationId", ctx.Response.Headers[Headers.CorrelationId].ToString());
        diag.Set("TraceId", ctx.TraceIdentifier);
        diag.Set("UserName", ctx.User?.Identity?.Name ?? "anon");
        diag.Set("Path", ctx.Request.Path.Value);
        diag.Set("Method", ctx.Request.Method);
        diag.Set("ClientIp", ctx.Connection.RemoteIpAddress?.ToString());
        diag.Set("StatusCode", ctx.Response.StatusCode);
    };
    opts.GetLevel = (httpContext, elapsed, ex) =>
        ex != null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
});

// output caching
app.UseOutputCache();

// Error handling / ProblemDetails
app.UseExceptionHandler(_ => { }); // minimal; AddProblemDetails handles mapping

// Correlation + Idempotency
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

//Serilog
app.UseMiddleware<SerilogHttpEnricherMiddleware>(); // push context into Serilog

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Root ping
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }))
   .WithTags("System");

app.Run();

