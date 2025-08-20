using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;

namespace UniEnroll.Infrastructure;

public sealed class SqlConnectionFactory(IConfiguration config, ILogger<SqlConnectionFactory> logger) : IDbConnectionFactory
{
    private static readonly ResiliencePipeline _openRetry = new ResiliencePipelineBuilder()
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<SqlException>(),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(200),
            MaxRetryAttempts = 5
        }).Build();

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        var cs = Environment.GetEnvironmentVariable("DB_CONNECTION")
                 ?? config.GetConnectionString("Default")
                 ?? "Server=localhost;Database=UniEnroll;Integrated Security=True;TrustServerCertificate=True;";

        var conn = new SqlConnection(cs);
        await _openRetry.ExecuteAsync(async _ =>
        {
            await conn.OpenAsync(ct);
        });
        return conn;
    }
}
