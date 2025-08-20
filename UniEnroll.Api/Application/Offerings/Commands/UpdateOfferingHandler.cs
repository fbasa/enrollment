using FluentValidation;
using MediatR;
using UniEnroll.Api.Caching.CacheInvalidator;
using UniEnroll.Api.Common;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Api.Validation;
using UniEnroll.Domain.Request;

namespace UniEnroll.Api.Application.Offerings.Commands;

public record UpdateOfferingCommand(long Id, string ETag, OfferingUpsertRequest Request) : IRequest<bool>, ITransactionalRequest;

public sealed class UpdateOfferingValidator : AbstractValidator<UpdateOfferingCommand>
{
    public UpdateOfferingValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ETag).NotEmpty();
        RuleFor(x => x.Request).SetValidator(new OfferingUpsertValidator());
    }
}

public sealed class UpdateOfferingHandler(IMediator mediator, IOfferingsRepository repo)
    : IRequestHandler<UpdateOfferingCommand, bool>
{
    public async Task<bool> Handle(UpdateOfferingCommand cmd, CancellationToken ct)
    {
        var result = await repo.UpdateAsync(cmd.Id, cmd.Request, Convert.FromBase64String(cmd.ETag), ct);
        await mediator.Publish(new OfferingChanged(cmd.Id), ct);
        return result;
    }
}
