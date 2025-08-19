using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace UniEnroll.Api.Observability;

public static class OpenTelemetrySetup
{
    public static void ConfigureTracing(TracerProviderBuilder t) =>
        t.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation();
         //.AddSqlClientInstrumentation();

    public static void ConfigureMetrics(MeterProviderBuilder m) =>
        m.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation();
}
