using Asp.Versioning;

namespace UniEnroll.Api.Versioning;

public static class ApiVersioningConfig
{
    public static IServiceCollection AddApiVersioningV1(this IServiceCollection services)
    {
        services
            .AddApiVersioning(o =>
            {
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1);        //< --major - only, yields v1
                o.ReportApiVersions = true;
                o.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";   // <-- shows v1 (not v1.0)
                o.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
