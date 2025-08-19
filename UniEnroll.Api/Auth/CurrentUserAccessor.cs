using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Http;
using UniEnroll.Api.Infrastructure;

namespace UniEnroll.Api.Security;

public interface ICurrentUserAccessor
{
    string? UserName { get; }
    string? Subject { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    Task<long> GetUserIdAsync(CancellationToken ct); // resolves to dbo.[User].userId; cached per request
}

public sealed class CurrentUserAccessor(
    IHttpContextAccessor http,
    IDbConnectionFactory db) : ICurrentUserAccessor
{
    private long? _cachedUserId;

    public string? UserName => http.HttpContext?.User?.Identity?.Name;
    public string? Subject => http.HttpContext?.User?.FindFirstValue("sub");
    public bool IsAuthenticated => http.HttpContext?.User?.Identity?.IsAuthenticated == true;
    public IReadOnlyCollection<string> Roles =>
        http.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();

    public async Task<long> GetUserIdAsync(CancellationToken ct)
    {
        if (_cachedUserId.HasValue) return _cachedUserId.Value;

        var key = UserName ?? Subject ?? "ext:api";
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long?>(
            "SELECT TOP 1 userId FROM dbo.[User] WHERE username=@u OR externalSubject=@u",
            new { u = key });

        _cachedUserId = id ?? 1; // fallback to admin/system id
        return _cachedUserId.Value;
    }
}
