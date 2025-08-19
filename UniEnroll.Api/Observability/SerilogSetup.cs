using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace UniEnroll.Api.Observability;

public static class SerilogSetup
{
    public static void Configure(LoggerConfiguration cfg, IConfiguration config)
    {
        cfg.ReadFrom.Configuration(config);

        // sensible defaults if not overridden in appsettings
        cfg.MinimumLevel.Information()
          .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
          .Enrich.FromLogContext()
          .WriteTo.Console();
    }
}
