using Microsoft.AspNetCore.OutputCaching;

namespace UniEnroll.Api.Caching;

public static class OutputCachePolicies
{
    /// <summary>
    /// Central place to register ALL output-cache policies/tags.
    /// Reads optional TTL overrides from config, else uses sensible defaults.
    /// </summary>
    public static IServiceCollection AddOutputCachingWithPolicies(this IServiceCollection services, IConfiguration cfg)
    {
        var termsTtl = GetSeconds(cfg, "Caching:TermsListTtlSeconds", 600); // 10m default
        var listTtl = GetSeconds(cfg, "Caching:ListTtlSeconds", 30);  // 30s default
        var detailTtl = GetSeconds(cfg, "Caching:DetailTtlSeconds", 60);  // 60s default

        services.AddOutputCache(o =>
        {
            // Terms listing — tag so we can evict on writes
            o.AddPolicy(OutputCachePolicyNames.TermsList, b => b
                .Expire(TimeSpan.FromSeconds(termsTtl))
                .Tag(OutputCachePolicyNames.TermsTag)
                .SetVaryByQuery("page", "pageSize")); // add more varies if needed

            // Generic list/detail examples you can reuse across endpoints
            o.AddPolicy(OutputCachePolicyNames.List30s, b => b
                .Expire(TimeSpan.FromSeconds(listTtl))
                .SetVaryByQuery("search", "departmentId", "termId", "courseId", "page", "pageSize"));

            o.AddPolicy(OutputCachePolicyNames.Detail60s, b => b
                .Expire(TimeSpan.FromSeconds(detailTtl)));
        });

        return services;
    }

    private static int GetSeconds(IConfiguration cfg, string key, int fallback)
        => int.TryParse(cfg[key], out var v) && v > 0 ? v : fallback;
}