using MediatR;
using UniEnroll.Api.Caching;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Offerings.Queries;

public record ListOfferingsQuery(long? TermId, long? CourseId, int Page, int PageSize)
    : IRequest<PageResult<OfferingListItemDto>>, ICacheableQuery
{
    public string CacheKey => $"offerings:list:t{TermId}:c{CourseId}:p{Page}:s{PageSize}";
    public TimeSpan? Ttl => TimeSpan.FromSeconds(30);
}

public sealed class ListOfferingsHandler(IOfferingsRepository repo)
    : IRequestHandler<ListOfferingsQuery, PageResult<OfferingListItemDto>>
{
    public Task<PageResult<OfferingListItemDto>> Handle(ListOfferingsQuery q, CancellationToken ct)
        => repo.ListAsync(q.TermId, q.CourseId, q.Page, q.PageSize, ct);
}