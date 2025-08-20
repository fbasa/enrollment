using Microsoft.Data.SqlClient;

namespace UniEnroll.Infrastructure.Transactions;

public interface IDbSession
{
    SqlConnection? Connection { get; }
    SqlTransaction? Transaction { get; }
    void Use(SqlConnection conn, SqlTransaction tx);
    void Clear();
}

public sealed class DbSession : IDbSession
{
    public SqlConnection? Connection { get; private set; }
    public SqlTransaction? Transaction { get; private set; }

    public void Use(SqlConnection conn, SqlTransaction tx)
    {
        Connection = conn; Transaction = tx;
    }

    public void Clear()
    {
        Connection = null; Transaction = null;
    }
}
