using Dapper;
using UniEnroll.Domain.Response;

namespace UniEnroll.Infrastructure.Repositories;

public interface IReportsRepository
{
    Task<IReadOnlyList<EnrollmentReportRow>> EnrollmentByCourseAsync(long? termId, CancellationToken ct);
    Task<IReadOnlyList<InstructorLoadRow>> InstructorLoadAsync(long? termId, CancellationToken ct);
    Task<IReadOnlyList<RoomUtilizationRow>> RoomUtilizationAsync(long? termId, CancellationToken ct);
}

public sealed class ReportsRepository(IDbConnectionFactory db) : IReportsRepository
{
    public async Task<IReadOnlyList<EnrollmentReportRow>> EnrollmentByCourseAsync(long? termId, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<EnrollmentReportRow>(SqlTemplates.EnrollmentByCourse, new { termId });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<InstructorLoadRow>> InstructorLoadAsync(long? termId, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<InstructorLoadRow>(SqlTemplates.InstructorLoad, new { termId });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<RoomUtilizationRow>> RoomUtilizationAsync(long? termId, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<RoomUtilizationRow>(SqlTemplates.RoomUtilization, new { termId });
        return rows.ToList();
    }
}
