using MediatR;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Payments.Queries;

public record ListInvoicesQuery(long? StudentId, long? TermId, int Page, int PageSize) : IRequest<PageResult<InvoiceDto>>;

public sealed class ListInvoicesHandler(IPaymentsRepository repo)
    : IRequestHandler<ListInvoicesQuery, PageResult<InvoiceDto>>
{
    public Task<PageResult<InvoiceDto>> Handle(ListInvoicesQuery q, CancellationToken ct)
        => repo.ListInvoicesAsync(q.StudentId, q.TermId, q.Page, q.PageSize, ct);
}
