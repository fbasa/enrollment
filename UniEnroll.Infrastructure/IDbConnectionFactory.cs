using Microsoft.Data.SqlClient;

namespace UniEnroll.Infrastructure;

public interface IDbConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct);
}
