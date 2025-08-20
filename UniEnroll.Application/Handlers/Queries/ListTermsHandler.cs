using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Response;
using UniEnroll.Application.Caching;

namespace UniEnroll.Application.Handlers.Queries;

public record ListTermsQuery : IRequest<IReadOnlyList<TermResponse>>, ICacheableQuery
{
    public string CacheKey => OutputCachePolicyNames.TermsList;

    public TimeSpan? Ttl => TimeSpan.FromSeconds(30);
}

public sealed class ListTermsHandler(ITermsRepository repo) : IRequestHandler<ListTermsQuery, IReadOnlyList<TermResponse>>
{
    public Task<IReadOnlyList<TermResponse>> Handle(ListTermsQuery request, CancellationToken ct) => repo.GetAllAsync(ct);
}

//TODO: Invalidate cached 
// e.g., in
//      CreateTermHandler
//      UpdateTermHandler
//      DeleteTermHandler
// after the DB write:
// await mediator.Publish(new TermChanged(), ct);