using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace UniEnroll.Api.Security;

public static class JwtBearerSetup
{
    /// <summary>
    /// Adds JWT bearer auth. If Auth:Authority exists → OIDC mode.
    /// Otherwise, if Auth:Dev:Enabled=true → local symmetric key mode.
    /// </summary>
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var authority = cfg["Auth:Authority"];
                var audience = cfg["Auth:Audience"];
                var useHttps = cfg.GetValue("Auth:RequireHttpsMetadata", true);

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",                // preferred
                    RoleClaimType = ClaimTypes.Role,       // our policies use roles
                };

                if (!string.IsNullOrWhiteSpace(authority))
                {
                    // OIDC authority (Auth0, Azure AD, etc.)
                    options.Authority = authority;
                    if (!string.IsNullOrWhiteSpace(audience))
                        options.Audience = audience;

                    options.RequireHttpsMetadata = useHttps;
                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(audience);
                }
                else if (cfg.GetValue("Auth:Dev:Enabled", false))
                {
                    // Dev symmetric key (HS256)
                    var issuer = cfg["Auth:Dev:Issuer"] ?? "unienroll-dev";
                    var aud = cfg["Auth:Dev:Audience"] ?? "unienroll-api";
                    var keyBytes = Encoding.UTF8.GetBytes(cfg["Auth:Dev:SigningKey"] ?? "change-this-dev-signing-key-32b-min!");
                    var key = new SymmetricSecurityKey(keyBytes);

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = aud,
                        ValidateLifetime = true,
                        NameClaimType = "name",
                        RoleClaimType = ClaimTypes.Role
                    };
                }
                else
                {
                    // No auth configured → accept no tokens
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true
                    };
                }

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        // keep logs, avoid leaking details to clients (GlobalExceptionHandler handles responses)
                        Microsoft.Extensions.Logging.LoggerExtensions.LogWarning(ctx.HttpContext.RequestServices
                            .GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                            .CreateLogger("JwtBearer"), ctx.Exception, "JWT authentication failed");
                        return Task.CompletedTask;
                    }
                };
            });

        // Default policy: require authenticated user via JWT
        services.AddAuthorization(o =>
        {
            o.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build();
        });

        return services;
    }

    /// <summary>Adds Swagger security scheme for Bearer tokens.</summary>
    public static IServiceCollection AddSwaggerJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }
}
