using MediatR;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Reports.Queries;

public record EnrollmentReportQuery(long? TermId) : IRequest<IReadOnlyList<EnrollmentReportRow>>;

public sealed class EnrollmentReportHandler(IReportsRepository repo)
    : IRequestHandler<EnrollmentReportQuery, IReadOnlyList<EnrollmentReportRow>>
{
    public Task<IReadOnlyList<EnrollmentReportRow>> Handle(EnrollmentReportQuery q, CancellationToken ct)
        => repo.EnrollmentByCourseAsync(q.TermId, ct);
}

