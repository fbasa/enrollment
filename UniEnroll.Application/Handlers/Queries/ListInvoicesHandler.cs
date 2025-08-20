using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Response;

namespace UniEnroll.Application.Handlers.Queries;

public record ListInvoicesQuery(long? StudentId, long? TermId, int Page, int PageSize) : IRequest<PageResult<InvoiceResponse>>;

public sealed class ListInvoicesHandler(IPaymentsRepository repo)
    : IRequestHandler<ListInvoicesQuery, PageResult<InvoiceResponse>>
{
    public Task<PageResult<InvoiceResponse>> Handle(ListInvoicesQuery q, CancellationToken ct)
        => repo.ListInvoicesAsync(q.StudentId, q.TermId, q.Page, q.PageSize, ct);
}
