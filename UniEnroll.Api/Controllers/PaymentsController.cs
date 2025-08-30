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
[Route("api/v{version:apiVersion}")]
//[Authorize(Roles = Roles.Admin + "," + Roles.Registrar)]
public sealed class PaymentsController(IMediator mediator, IConfiguration cfg) : ControllerBase
{
    private bool PaymentsEnabled => cfg.GetSection("App").GetValue<bool?>("PaymentsEnabled").GetValueOrDefault(true);

    [HttpGet("invoices")]
    public async Task<ActionResult<PageResult<InvoiceResponse>>> List([FromQuery] long? studentId, [FromQuery] long? termId, CancellationToken ct)
    {
        if (!PaymentsEnabled) return Forbid();
        var page = Pagination.PageRequest.From(HttpContext, 20, 100);
        var res = await mediator.Send(new ListInvoicesQuery(studentId, termId, page.Page, page.PageSize), ct);
        Pagination.WriteLinkHeaders(HttpContext, page.Page, page.PageSize, res.TotalCount);
        return Ok(res);
    }

    [HttpPost("invoices")]
    public async Task<IResult> Create([FromBody] CreateInvoiceRequest req, CancellationToken ct)
    {
        if (!PaymentsEnabled) return Results.Forbid();
        var id = await mediator.Send(new CreateInvoiceCommand(req), ct);
        return Results.Created($"/api/v1/invoices/{id}", new { invoiceId = id });
    }

    [HttpPost("invoices/{id:long}/payments")]
    public async Task<IResult> Pay(long id, [FromBody] PaymentRequest req, CancellationToken ct)
    {
        if (!PaymentsEnabled) return Results.Forbid();
        _ = await mediator.Send(new PayInvoiceCommand(id, req), ct);
        return Results.Accepted($"/api/v1/invoices/{id}");
    }
}
