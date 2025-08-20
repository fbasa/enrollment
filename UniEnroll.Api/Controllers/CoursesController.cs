using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEnroll.Application.Common;
using UniEnroll.Application.Handlers.Commands;
using UniEnroll.Application.Handlers.Queries;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Request;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/courses")]
public sealed class CoursesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<CourseResponse>>> List([FromQuery] string? search, [FromQuery] long? departmentId, CancellationToken ct)
    {
        var page = Pagination.PageRequest.From(HttpContext, 20, 100);
        var result = await mediator.Send(new ListCoursesQuery(search, departmentId, page.Page, page.PageSize), ct);
        Pagination.WriteLinkHeaders(HttpContext, page.Page, page.PageSize, result.TotalCount);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CapacityOverride)]
    public async Task<IResult> Create([FromBody] CreateCourseRequest req, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateCourseCommand(req), ct);
        return Results.Created($"/api/v1/courses/{id}", new { courseId = id });
    }
}
