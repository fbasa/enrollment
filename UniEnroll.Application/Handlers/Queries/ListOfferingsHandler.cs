using MediatR;
using UniEnroll.Application.Caching;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Response;
using UniEnroll.Infrastructure.Repositories;

namespace UniEnroll.Application.Handlers.Queries;

public record ListOfferingsQuery(long? TermId, long? CourseId, int Page, int PageSize)
    : IRequest<PageResult<OfferingListItemResponse>>, ICacheableQuery
{
    public string CacheKey => $"offerings:list:t{TermId}:c{CourseId}:p{Page}:s{PageSize}";
    public TimeSpan? Ttl => TimeSpan.FromSeconds(30);
}

public sealed class ListOfferingsHandler(IOfferingsRepository repo)
    : IRequestHandler<ListOfferingsQuery, PageResult<OfferingListItemResponse>>
{
    public Task<PageResult<OfferingListItemResponse>> Handle(ListOfferingsQuery q, CancellationToken ct)
        => repo.ListAsync(q.TermId, q.CourseId, q.Page, q.PageSize, ct);
}