using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Application.Offerings.Queries;

public record GetOfferingQuery(long Id) : IRequest<OfferingDetailResponse?>;

public sealed class GetOfferingHandler(IOfferingsRepository repo)
    : IRequestHandler<GetOfferingQuery, OfferingDetailResponse?>
{
    public Task<OfferingDetailResponse?> Handle(GetOfferingQuery q, CancellationToken ct)
        => repo.GetAsync(q.Id, ct);
}