using Microsoft.Data.SqlClient;

namespace UniEnroll.Infrastructure.Transactions;

public sealed class UnitOfWork : IAsyncDisposable
{
    public SqlConnection Connection { get; }
    public SqlTransaction Transaction { get; }

    private UnitOfWork(SqlConnection conn, SqlTransaction tx)
    {
        Connection = conn; Transaction = tx;
    }

    public static async Task<UnitOfWork> BeginAsync(IDbConnectionFactory factory, CancellationToken ct)
    {
        var conn = await factory.CreateOpenConnectionAsync(ct);
        var tx = await conn.BeginTransactionAsync(ct);
        return new UnitOfWork(conn, (SqlTransaction)tx);
    }

    public Task CommitAsync(CancellationToken ct) => Transaction.CommitAsync(ct);
    public Task RollbackAsync(CancellationToken ct) => Transaction.RollbackAsync(ct);

    public async ValueTask DisposeAsync()
    {
        await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
