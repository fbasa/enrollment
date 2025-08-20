using MediatR;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Application.Reports.Queries;

public record RoomUtilizationQuery(long? TermId) : IRequest<IReadOnlyList<RoomUtilizationRow>>;

public sealed class RoomUtilizationHandler(IReportsRepository repo)
    : IRequestHandler<RoomUtilizationQuery, IReadOnlyList<RoomUtilizationRow>>
{
    public Task<IReadOnlyList<RoomUtilizationRow>> Handle(RoomUtilizationQuery q, CancellationToken ct)
        => repo.RoomUtilizationAsync(q.TermId, ct);
}

