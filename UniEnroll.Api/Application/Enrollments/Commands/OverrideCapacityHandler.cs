using MediatR;
using Microsoft.AspNetCore.Authorization;
using UniEnroll.Api.Common;
using UniEnroll.Api.Errors;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Enrollments.Commands;

public record OverrideCapacityCommand(long OfferingId, int NewCapacity, string Reason)
    : IRequest<Unit>, ITransactionalRequest;

public sealed class CapacityOverrideRequirement(long? DepartmentId = null) : IAuthorizationRequirement
{
    public long? DepartmentId { get; } = DepartmentId;
}

public sealed class OverrideCapacityHandler(
    IAuthorizationService authz,
    IHttpContextAccessor http,
    IOfferingsRepository offerings,
    ILogger<OverrideCapacityHandler> log)
    : IRequestHandler<OverrideCapacityCommand, Unit>
{
    public async Task<Unit> Handle(OverrideCapacityCommand cmd, CancellationToken ct)
    {
        var user = http.HttpContext!.User;
        var auth = await authz.AuthorizeAsync(user, resource: null, new CapacityOverrideRequirement());
        if (!auth.Succeeded) throw new UnauthorizedAccessException();

        // Fetch + update offering (reuse existing UpdateAsync with ETag if you want concurrency checks)
        var dto = await offerings.GetAsync(cmd.OfferingId, ct) ?? throw new NotFoundException("Offering not found.");

        var ok = await offerings.UpdateCapacityAsync(
                        cmd.OfferingId,
                        cmd.NewCapacity,
                        Convert.FromBase64String(dto.ETag),
                        ct);

        if (!ok) throw new ConflictException("Concurrency conflict while overriding capacity.");

        log.LogInformation("Capacity override by {User} for offering {OfferingId}. NewCapacity={NewCapacity}. Reason={Reason}",
            user.Identity?.Name, cmd.OfferingId, cmd.NewCapacity, cmd.Reason);

        return Unit.Value;
    }
}
