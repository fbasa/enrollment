using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using UniEnroll.Api.Infrastructure.Transactions;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Request;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Infrastructure.Repositories;

public interface IOfferingsRepository
{
    Task<PageResult<OfferingListItemResponse>> ListAsync(long? termId, long? courseId, int page, int pageSize, CancellationToken ct);
    Task<OfferingDetailResponse?> GetAsync(long id, CancellationToken ct);
    Task<long> CreateAsync(OfferingUpsertRequest req, CancellationToken ct);
    Task<bool> UpdateAsync(long id, OfferingUpsertRequest req, byte[] rowVersion, CancellationToken ct);
    Task<bool> UpdateCapacityAsync(long id, int capacity, byte[] rowVersion, CancellationToken ct);
}

public sealed class OfferingsRepository(IDbConnectionFactory db, IDbSession session) : IOfferingsRepository
{
    public async Task<PageResult<OfferingListItemResponse>> ListAsync(long? termId, long? courseId, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);

        var where = new List<string> { "1=1" };
        var p = new DynamicParameters();
        if (termId is > 0) { where.Add("o.termId=@termId"); p.Add("termId", termId); }
        if (courseId is > 0) { where.Add("o.courseId=@courseId"); p.Add("courseId", courseId); }
        p.Add("skip", (page - 1) * pageSize);
        p.Add("take", pageSize);

        var sql = $"""
        WITH counts AS (
          SELECT offeringId,
                 SUM(CASE WHEN status='Enrolled' THEN 1 ELSE 0 END) AS Enrolled,
                 SUM(CASE WHEN status='Waitlisted' THEN 1 ELSE 0 END) AS Waitlisted
          FROM dbo.Enrollment GROUP BY offeringId
        )
        SELECT o.offeringId AS OfferingId, t.code AS TermCode, c.code AS CourseCode, o.section AS Section,
               r.code AS RoomCode, o.capacity AS Capacity,
               ISNULL(cnt.Enrolled,0) AS Enrolled, ISNULL(cnt.Waitlisted,0) AS Waitlisted
        FROM dbo.CourseOffering o
        JOIN dbo.Term t ON t.termId=o.termId
        JOIN dbo.Course c ON c.courseId=o.courseId
        JOIN dbo.Room r ON r.roomId=o.roomId
        LEFT JOIN counts cnt ON cnt.offeringId=o.offeringId
        WHERE {string.Join(" AND ", where)}
        ORDER BY t.startDate DESC, c.code, o.section
        OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;

        SELECT COUNT(*) FROM dbo.CourseOffering o WHERE {string.Join(" AND ", where)};
        """;

        using var multi = await conn.QueryMultipleAsync(sql, p);
        var items = (await multi.ReadAsync<OfferingListItemResponse>()).ToList();
        var total = await multi.ReadFirstAsync<int>();
        return new PageResult<OfferingListItemResponse>(items, total);
    }

    public async Task<OfferingDetailResponse?> GetAsync(long id, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);

        var sql = """
        SELECT o.offeringId, t.code AS termCode, c.code AS courseCode, c.title,
               o.section, r.code AS roomCode, o.capacity, o.waitlistCapacity, o.rowversion
        FROM dbo.CourseOffering o
        JOIN dbo.Term t ON t.termId=o.termId
        JOIN dbo.Course c ON c.courseId=o.courseId
        JOIN dbo.Room r ON r.roomId=o.roomId
        WHERE o.offeringId=@id;

        SELECT ts.dayOfWeek, ts.startTime, ts.endTime
        FROM dbo.OfferingSchedule os
        JOIN dbo.TimeSlot ts ON ts.timeSlotId=os.timeSlotId
        WHERE os.offeringId=@id
        ORDER BY ts.dayOfWeek, ts.startTime;
        """;

        using var multi = await conn.QueryMultipleAsync(sql, new { id });
        var head = await multi.ReadFirstOrDefaultAsync();
        if (head == null) return null;

        var sched = (await multi.ReadAsync()).Select(r =>
            new ScheduleSlotResponse((int)r.dayOfWeek, TimeOnly.FromTimeSpan((TimeSpan)r.startTime), TimeOnly.FromTimeSpan((TimeSpan)r.endTime))
        ).ToList();

        var etag = Convert.ToBase64String((byte[])head.rowversion);
        return new OfferingDetailResponse(
            (long)head.offeringId, (string)head.termCode, (string)head.courseCode, (string)head.title,
            (string)head.section, (string)head.roomCode, (int)head.capacity, (int)head.waitlistCapacity, sched, etag);
    }

    public async Task<long> CreateAsync(OfferingUpsertRequest req, CancellationToken ct)
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
            var offeringId = await conn.ExecuteScalarAsync<long>(
                """
            INSERT dbo.CourseOffering(termId, courseId, section, roomId, capacity, waitlistCapacity)
            VALUES(@TermId, @CourseId, @Section, @RoomId, @Capacity, @WaitlistCapacity);
            SELECT SCOPE_IDENTITY();
            """, req, tx);

            // TVP for schedules
            var tvp = new DataTable();
            tvp.Columns.Add("timeSlotId", typeof(long));
            foreach (var id in req.TimeSlotIds) tvp.Rows.Add(id);
            var param = new SqlParameter("@tvp", tvp) { SqlDbType = SqlDbType.Structured, TypeName = "dbo.OfferingScheduleTvp" };

            await conn.ExecuteAsync(
                "INSERT dbo.OfferingSchedule(offeringId, timeSlotId) SELECT @offeringId, ts.timeSlotId FROM @tvp ts;",
                new { offeringId, tvp = param }, tx);

            await tx.CommitAsync(ct);
            return offeringId;
        }
        catch
        {
            if (localTx is not null) await localTx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(long id, OfferingUpsertRequest req, byte[] rowVersion, CancellationToken ct)
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
            var updated = await conn.ExecuteAsync(
            """
            UPDATE dbo.CourseOffering
               SET termId=@TermId, courseId=@CourseId, section=@Section,
                   roomId=@RoomId, capacity=@Capacity, waitlistCapacity=@WaitlistCapacity
             WHERE offeringId=@Id AND rowversion=@RowVersion;
            """,
            new { req.TermId, req.CourseId, req.Section, req.RoomId, req.Capacity, req.WaitlistCapacity, Id = id, RowVersion = rowVersion }, tx);

            if (updated == 0) { if (localTx is not null) await localTx.RollbackAsync(ct); return false; }

            await conn.ExecuteAsync("DELETE FROM dbo.OfferingSchedule WHERE offeringId=@Id", new { Id = id }, tx);

            var tvp = new DataTable();
            tvp.Columns.Add("timeSlotId", typeof(long));
            foreach (var ts in req.TimeSlotIds) tvp.Rows.Add(ts);
            var param = new SqlParameter("@tvp", tvp) { SqlDbType = SqlDbType.Structured, TypeName = "dbo.OfferingScheduleTvp" };

            await conn.ExecuteAsync(
                "INSERT dbo.OfferingSchedule(offeringId, timeSlotId) SELECT @Id, ts.timeSlotId FROM @tvp ts;",
                new { Id = id, tvp = param }, tx);

            if (localTx is not null) await localTx.CommitAsync(ct);
            return true;
        }
        catch
        {
            if (localTx is not null) await localTx.RollbackAsync(ct);
            throw;
        }
    }
    public async Task<bool> UpdateCapacityAsync(long id, int capacity, byte[] rowVersion, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        // participate in ambient transaction if present
        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.CourseOffering SET capacity=@Capacity WHERE offeringId=@Id AND rowversion=@RowVersion;",
            new { Capacity = capacity, Id = id, RowVersion = rowVersion },
            session.Transaction);
        return rows > 0;
    }

}
