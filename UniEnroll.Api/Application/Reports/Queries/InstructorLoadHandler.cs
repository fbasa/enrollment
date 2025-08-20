using MediatR;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Application.Reports.Queries;

public record InstructorLoadQuery(long? TermId) : IRequest<IReadOnlyList<InstructorLoadRow>>;

public sealed class InstructorLoadHandler(IReportsRepository repo)
    : IRequestHandler<InstructorLoadQuery, IReadOnlyList<InstructorLoadRow>>
{
    public Task<IReadOnlyList<InstructorLoadRow>> Handle(InstructorLoadQuery q, CancellationToken ct)
        => repo.InstructorLoadAsync(q.TermId, ct);
}

