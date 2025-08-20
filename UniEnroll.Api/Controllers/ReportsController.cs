using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using UniEnroll.Api.Common;
using UniEnroll.Application.Handlers.Queries;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Roles = Roles.Admin + "," + Roles.Registrar)]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("enrollment")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentReportRow>>> Enrollment([FromQuery] long? termId, CancellationToken ct)
        => Ok(await mediator.Send(new EnrollmentReportQuery(termId), ct));

    [HttpGet("instructor-load")]
    public async Task<ActionResult<IReadOnlyList<InstructorLoadRow>>> InstructorLoad([FromQuery] long? termId, CancellationToken ct)
        => Ok(await mediator.Send(new InstructorLoadQuery(termId), ct));

    [HttpGet("room-utilization")]
    public async Task<ActionResult<IReadOnlyList<RoomUtilizationRow>>> RoomUtil([FromQuery] long? termId, CancellationToken ct)
        => Ok(await mediator.Send(new RoomUtilizationQuery(termId), ct));

    [HttpGet("enrollment.csv")]
    public async Task<IActionResult> EnrollmentCsv([FromQuery] long? termId, CancellationToken ct)
    {
        var rows = await mediator.Send(new EnrollmentReportQuery(termId), ct);
        var sb = new StringBuilder();
        sb.AppendLine("TermCode,CourseCode,Title,Enrolled,Waitlisted");
        foreach (var r in rows) sb.AppendLine($"{r.TermCode},{r.CourseCode},\"{r.Title}\",{r.EnrolledCount},{r.WaitlistedCount}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "enrollment-by-course.csv");
    }
}
