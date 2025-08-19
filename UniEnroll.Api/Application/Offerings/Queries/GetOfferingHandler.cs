using MediatR;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Offerings.Queries;

public record GetOfferingQuery(long Id) : IRequest<OfferingDetailDto?>;

public sealed class GetOfferingHandler(IOfferingsRepository repo)
    : IRequestHandler<GetOfferingQuery, OfferingDetailDto?>
{
    public Task<OfferingDetailDto?> Handle(GetOfferingQuery q, CancellationToken ct)
        => repo.GetAsync(q.Id, ct);
}