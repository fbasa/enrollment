using FluentValidation;
using MediatR;
using UniEnroll.Api.Common;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Api.Caching.CacheInvalidator;
using UniEnroll.Api.Validation;

namespace UniEnroll.Api.Application.Offerings.Commands;

public record CreateOfferingCommand(OfferingUpsertRequest Request) : IRequest<long>, ITransactionalRequest;

public sealed class CreateOfferingValidator : AbstractValidator<CreateOfferingCommand>
{
    public CreateOfferingValidator() => RuleFor(x => x.Request).SetValidator(new OfferingUpsertValidator());
}


public sealed class CreateOfferingHandler(IMediator mediator, IOfferingsRepository repo)
    : IRequestHandler<CreateOfferingCommand, long>
{
    public async Task<long> Handle(CreateOfferingCommand cmd, CancellationToken ct)
    {
        var newId = await repo.CreateAsync(cmd.Request, ct);
        await mediator.Publish(new OfferingChanged(newId), ct);       // invalidate cached
        return newId;
    }
}
