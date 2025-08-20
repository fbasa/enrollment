using Asp.Versioning;
using Dapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEnroll.Api.Application.Enrollments.Commands;
using UniEnroll.Api.Common;
using UniEnroll.Api.Infrastructure;
using UniEnroll.Domain.Request;

namespace UniEnroll.Api.Application.Enrollments;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/enrollments")]
public sealed class EnrollmentsController(IDbConnectionFactory db, IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Student + "," + Roles.Registrar + "," + Roles.Admin)]
    public async Task<IResult> Enroll([FromBody] EnrollRequest req, CancellationToken ct)
    {
        var actorId = await GetActorUserIdAsync(ct);
        var resp = await mediator.Send(new EnrollCommand(req, actorId), ct);
        return Results.Ok(resp);
    }

    [HttpPost("{id:long}/drop")]
    [Authorize(Roles = Roles.Student + "," + Roles.Registrar + "," + Roles.Admin)]
    public async Task<IResult> Drop(long id, [FromBody] DropRequest _req, CancellationToken ct)
    {
        var actorId = await GetActorUserIdAsync(ct);
        var ok = await mediator.Send(new DropCommand(id, actorId), ct);
        return ok ? Results.NoContent() : ProblemDetailsExtensions.DomainProblem("Cannot drop", "Only Enrolled records can be dropped.", StatusCodes.Status409Conflict);
    }

    [HttpPost("{id:long}/capacity-override")]
    [Authorize(Policy = Policies.CapacityOverride)]
    public async Task<IResult> CapacityOverride(long id, [FromBody] CapacityOverrideRequest req, CancellationToken ct)
    {
        await mediator.Send(new OverrideCapacityCommand(id, req.NewCapacity, req.Reason), ct);
        return Results.NoContent();
    }

    [HttpPost("waive-prereq")]
    [Authorize(Policy = Policies.PrereqWaiver)]
    public async Task<IResult> WaivePrereq([FromBody] WaivePrereqRequest req, CancellationToken ct)
    {
        // Delegate to MediatR command (below). This bypasses validation checks.
        var actorId = await GetActorUserIdAsync(ct);
        var resp = await mediator.Send(new WaivePrereqCommand(req.StudentId, req.OfferingId, req.Reason, actorId), ct);
        return Results.Ok(resp); // Enrolled
    }


    private async Task<long> GetActorUserIdAsync(CancellationToken ct)
    {
        var uname = User.Identity?.Name ?? User.Claims.FirstOrDefault(c => c.Type is "sub")?.Value ?? "ext:api";
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long?>("SELECT TOP 1 userId FROM dbo.[User] WHERE username=@u OR externalSubject=@u", new { u = uname });
        return id ?? 1;
    }
}
