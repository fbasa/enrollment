using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Response;

namespace UniEnroll.Application.Handlers.Queries;

public record RoomUtilizationQuery(long? TermId) : IRequest<IReadOnlyList<RoomUtilizationRow>>;

public sealed class RoomUtilizationHandler(IReportsRepository repo)
    : IRequestHandler<RoomUtilizationQuery, IReadOnlyList<RoomUtilizationRow>>
{
    public Task<IReadOnlyList<RoomUtilizationRow>> Handle(RoomUtilizationQuery q, CancellationToken ct)
        => repo.RoomUtilizationAsync(q.TermId, ct);
}

