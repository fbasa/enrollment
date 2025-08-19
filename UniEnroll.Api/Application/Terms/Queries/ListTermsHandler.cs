using MediatR;
using UniEnroll.Api.Caching;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Terms.Queries;

public record ListTermsQuery : IRequest<IReadOnlyList<TermDto>>, ICacheableQuery
{
    public string CacheKey => OutputCachePolicyNames.TermsList;

    public TimeSpan? Ttl => TimeSpan.FromSeconds(30);
}

public sealed class ListTermsHandler(ITermsRepository repo) : IRequestHandler<ListTermsQuery, IReadOnlyList<TermDto>>
{
    public Task<IReadOnlyList<TermDto>> Handle(ListTermsQuery request, CancellationToken ct) => repo.GetAllAsync(ct);
}

//TODO: Invalidate cached 
// e.g., in
//      CreateTermHandler
//      UpdateTermHandler
//      DeleteTermHandler
// after the DB write:
// await mediator.Publish(new TermChanged(), ct);