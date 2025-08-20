using Dapper;
using Microsoft.Data.SqlClient;
using UniEnroll.Api.Infrastructure.Transactions;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Request;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Infrastructure.Repositories;

public interface ICoursesRepository
{
    Task<long> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    Task<PageResult<CourseResponse>> ListAsync(string? search, long? departmentId, int page, int pageSize, CancellationToken ct);
}

public sealed class CoursesRepository(IDbConnectionFactory db, IDbSession session) : ICoursesRepository
{
    public async Task<long> CreateAsync(CreateCourseRequest req, CancellationToken ct)
    {
        //await using var conn = await db.CreateOpenConnectionAsync(ct);
        //using var tx = await conn.BeginTransactionAsync(ct);

        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        var tx = session.Transaction;

        SqlTransaction? localTx = null;
        if (tx is null) localTx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
        var effTx = tx ?? localTx!;

        try
        {
            var courseId = await conn.ExecuteScalarAsync<long>(
                """
                INSERT dbo.Course(termId, courseId, section, roomId, capacity, waitlistCapacity)
                VALUES(@TermId, @CourseId, @Section, @RoomId, @Capacity, @WaitlistCapacity);
                SELECT SCOPE_IDENTITY();
                """, req, tx);

            await effTx.CommitAsync(ct);
            return courseId;
        }
        catch
        {
            if (localTx is not null) await localTx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<PageResult<CourseResponse>> ListAsync(string? search, long? departmentId, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);

        var where = new List<string> { "1=1" };
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search)) 
        { 
            where.Add("(c.code LIKE @q OR c.title LIKE @q)"); 
            p.Add("q", $"%{search}%"); 
        }

        if (departmentId is > 0) 
        { 
            where.Add("c.departmentId=@dept"); 
            p.Add("dept", departmentId); 
        }

        p.Add("skip", (page - 1) * pageSize);
        p.Add("take", pageSize);

        var sql = $"""
            SELECT c.courseId AS CourseId, c.code AS Code, c.title AS Title, c.units AS Units, d.code AS DepartmentCode
            FROM dbo.Course c JOIN dbo.Department d ON d.departmentId=c.departmentId
            WHERE {string.Join(" AND ", where)}
            ORDER BY c.code
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;

            SELECT COUNT(1) FROM dbo.Course c WHERE {string.Join(" AND ", where)};
        """;

        using var multi = await conn.QueryMultipleAsync(sql, p);
        var items = (await multi.ReadAsync<CourseResponse>()).ToList();
        var total = await multi.ReadFirstAsync<int>();
        return new PageResult<CourseResponse>(items, total);
    }
}
