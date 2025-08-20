using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniEnroll.Api.Common;
using UniEnroll.Application.Handlers.Commands;
using UniEnroll.Application.Handlers.Queries;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Request;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/offerings")]
public sealed class OfferingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<OfferingListItemResponse>>> List([FromQuery] long? termId, [FromQuery] long? courseId, CancellationToken ct)
    {
        var page = Pagination.PageRequest.From(HttpContext, 20, 100);
        var result = await mediator.Send(new ListOfferingsQuery(termId, courseId, page.Page, page.PageSize), ct);
        Pagination.WriteLinkHeaders(HttpContext, page.Page, page.PageSize, result.TotalCount);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OfferingDetailResponse>> Get(long id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetOfferingQuery(id), ct);
        if (dto is null) return NotFound();
        Response.Headers.ETag = $"W/\"{dto.ETag}\"";
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CapacityOverride)]
    public async Task<IResult> Create([FromBody] OfferingUpsertRequest req, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateOfferingCommand(req), ct);
        return Results.Created($"/api/v1/offerings/{id}", new { offeringId = id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Policies.CapacityOverride)]
    public async Task<IResult> Update(long id, [FromBody] OfferingUpsertRequest req, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(Headers.IfMatch, out var etagHeader))
            return Results.Problem(title: "Missing If-Match", detail: "Optimistic concurrency required.", statusCode: StatusCodes.Status428PreconditionRequired);

        var etag = etagHeader.ToString().Trim('"').TrimStart('W', '/').Trim('"');
        var ok = await mediator.Send(new UpdateOfferingCommand(id, etag, req), ct);
        return ok ? Results.NoContent() : Results.StatusCode(StatusCodes.Status409Conflict);
    }
}
