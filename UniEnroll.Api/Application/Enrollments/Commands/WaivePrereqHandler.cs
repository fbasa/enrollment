using MediatR;
using Microsoft.AspNetCore.Authorization;
using UniEnroll.Api.Common;
using UniEnroll.Api.Errors;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Api.Security;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Application.Enrollments.Commands;

public record WaivePrereqCommand(long StudentId, long OfferingId, string Reason, long ActorUserId)
    : IRequest<EnrollResponse>, ITransactionalRequest;

public sealed class WaivePrereqHandler(
    IAuthorizationService authz,
    IHttpContextAccessor http,
    IEnrollmentsRepository repo,
    ILogger<WaivePrereqHandler> log)
    : IRequestHandler<WaivePrereqCommand, EnrollResponse>
{
    public async Task<EnrollResponse> Handle(WaivePrereqCommand cmd, CancellationToken ct)
    {
        var user = http.HttpContext!.User;

        // policy check here (even if the controller had [Authorize], this keeps it safe for other callers)
        var auth = await authz.AuthorizeAsync(user, resource: null, new PrereqWaiverRequirement());
        if (!auth.Succeeded) throw new UnauthorizedAccessException();

        // set actor for auditing SQL trigger/SP (we already use this pattern)
        await repo.SetActorAsync(cmd.ActorUserId, ct);

        // Seat snapshot & insert directly as Enrolled (bypass prereq validation on purpose)
        var seat = await repo.SeatSnapshotForUpdateAsync(cmd.OfferingId, ct);
        if (seat is null) throw new NotFoundException("Offering not found.");
        var (capacity, waitCap, enrolled, waitlisted) = seat.Value;

        if (enrolled >= capacity && waitlisted >= waitCap)
            throw new ConflictException("Class capacity and waitlist are full.");

        var status = enrolled < capacity ? "Enrolled" : "Waitlisted";
        var id = await repo.CreateAsync(cmd.StudentId, cmd.OfferingId, status, ct);

        // Record the reason in audit trail (reuse EnrollmentAudit via existing trigger or an explicit insert—see note)
        log.LogInformation("Prereq waiver applied by {User} for student {StudentId}, offering {OfferingId}. Reason: {Reason}",
            user.Identity?.Name, cmd.StudentId, cmd.OfferingId, cmd.Reason);

        return new EnrollResponse(id, status);
    }
}
