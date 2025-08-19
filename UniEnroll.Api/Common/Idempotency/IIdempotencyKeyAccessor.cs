namespace UniEnroll.Api.Common.Idempotency;

public interface IIdempotencyKeyAccessor
{
    /// Returns (key, method, path, userId-for-scope)
    (string? Key, string Method, string Path, string UserScope) Read();
}
