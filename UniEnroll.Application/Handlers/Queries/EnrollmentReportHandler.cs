using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Response;

namespace UniEnroll.Application.Handlers.Queries;

public record EnrollmentReportQuery(long? TermId) : IRequest<IReadOnlyList<EnrollmentReportRow>>;

public sealed class EnrollmentReportHandler(IReportsRepository repo)
    : IRequestHandler<EnrollmentReportQuery, IReadOnlyList<EnrollmentReportRow>>
{
    public Task<IReadOnlyList<EnrollmentReportRow>> Handle(EnrollmentReportQuery q, CancellationToken ct)
        => repo.EnrollmentByCourseAsync(q.TermId, ct);
}

