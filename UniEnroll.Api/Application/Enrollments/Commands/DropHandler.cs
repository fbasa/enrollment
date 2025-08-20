using MediatR;
using UniEnroll.Api.Common;
using UniEnroll.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Enrollments.Commands;

public record DropCommand(long EnrollmentId, long ActorUserId) : IRequest<bool>, ITransactionalRequest;

public sealed class DropHandler(IEnrollmentsRepository repo) : IRequestHandler<DropCommand, bool>
{
    public async Task<bool> Handle(DropCommand cmd, CancellationToken ct)
    {
        await repo.SetActorAsync(cmd.ActorUserId, ct);

        var changed = await repo.DropAsync(cmd.EnrollmentId, ct);
        if (!changed) return false;

        var promote = await repo.FirstWaitlistedAsync(cmd.EnrollmentId, ct);
        if (promote.HasValue) await repo.PromoteAsync(promote.Value, ct);

        return true;
    }
}