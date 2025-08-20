using Dapper;
using UniEnroll.Infrastructure.Transactions;

namespace UniEnroll.Infrastructure.Repositories;

public interface IEnrollmentsRepository
{
    Task SetActorAsync(long actorUserId, CancellationToken ct);
    Task<IReadOnlyList<string>> ValidateAsync(long studentId, long offeringId, CancellationToken ct);
    Task<(int capacity, int waitCap, int enrolled, int waitlisted)?> SeatSnapshotForUpdateAsync(long offeringId, CancellationToken ct);
    Task<long> CreateAsync(long studentId, long offeringId, string status, CancellationToken ct);
    Task<bool> DropAsync(long enrollmentId, CancellationToken ct);
    Task<long?> FirstWaitlistedAsync(long enrollmentId, CancellationToken ct);
    Task PromoteAsync(long enrollmentId, CancellationToken ct);
}


public sealed class EnrollmentsRepository(IDbConnectionFactory db, IDbSession session) : IEnrollmentsRepository
{
    public async Task SetActorAsync(long actorUserId, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("EXEC dbo.sp_set_actor_context @actorUserId", new { actorUserId }, session.Transaction);
    }

    public async Task<IReadOnlyList<string>> ValidateAsync(long studentId, long offeringId, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        var errors = await conn.QueryAsync<string>(SqlTemplates.ValidateStudent, new { StudentId = studentId, OfferingId = offeringId }, session.Transaction);
        return errors.ToList();
    }

    public async Task<(int capacity, int waitCap, int enrolled, int waitlisted)?> SeatSnapshotForUpdateAsync(long offeringId, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<(int, int, int, int)?>(SqlTemplates.SeatSnapshotForUpdate, new { OfferingId = offeringId }, session.Transaction);
    }

    public async Task<long> CreateAsync(long studentId, long offeringId, string status, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            "INSERT dbo.Enrollment(offeringId, studentId, status) VALUES(@OfferingId,@StudentId,@Status); SELECT SCOPE_IDENTITY();",
            new { OfferingId = offeringId, StudentId = studentId, Status = status }, session.Transaction);
    }

    public async Task<bool> DropAsync(long enrollmentId, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        var changed = await conn.ExecuteAsync(
            "UPDATE dbo.Enrollment SET status='Dropped', updatedAtUtc=SYSUTCDATETIME() WHERE enrollmentId=@Id AND status='Enrolled';",
            new { Id = enrollmentId }, session.Transaction);
        return changed > 0;
    }

    public async Task<long?> FirstWaitlistedAsync(long enrollmentId, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<long?>(SqlTemplates.FirstWaitlisted, new { Id = enrollmentId }, session.Transaction);
    }

    public async Task PromoteAsync(long enrollmentId, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE dbo.Enrollment SET status='Enrolled', updatedAtUtc=SYSUTCDATETIME() WHERE enrollmentId=@Id;",
            new { Id = enrollmentId }, session.Transaction);
    }
}
