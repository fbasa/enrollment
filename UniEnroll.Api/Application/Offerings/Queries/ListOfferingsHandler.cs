using MediatR;
using UniEnroll.Api.Caching;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Application.Offerings.Queries;

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