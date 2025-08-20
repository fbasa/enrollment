using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using UniEnroll.Application.Handlers.Commands;
using UniEnroll.Application.Handlers.Queries;
using UniEnroll.Domain.Request;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/terms")]
public sealed class TermsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [OutputCache(PolicyName = "terms-list")]
    public async Task<ActionResult<IReadOnlyList<TermResponse>>> Get(CancellationToken ct) 
        => Ok(await mediator.Send(new ListTermsQuery(), ct));

    [HttpPost]
    public async Task<IResult> CreateAsync([FromBody] CreateTermRequest req, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateOrUpdateTermCommand(req), ct);
        return Results.Created($"/api/v1/terms/{id}", new { termId  = id });
    }
}
