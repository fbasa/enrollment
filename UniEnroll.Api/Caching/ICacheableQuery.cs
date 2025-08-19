namespace UniEnroll.Api.Caching;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Ttl { get; }
}


