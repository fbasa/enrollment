using MediatR;
using UniEnroll.Api.Caching.CacheInvalidator;
using UniEnroll.Api.Infrastructure.Repositories;
using UniEnroll.Domain.Request;

namespace UniEnroll.Api.Application.Terms.Commands;

public record CreateOrUpdateTermCommand(CreateTermRequest Request) : IRequest<long>;

public sealed class CreateOrUpdateTermHandler(IMediator mediator, ITermsRepository repo) : IRequestHandler<CreateOrUpdateTermCommand, long>
{
    public async Task<long> Handle(CreateOrUpdateTermCommand cmd, CancellationToken ct)
    {
        var newId = await repo.CreateAsync(cmd.Request, ct);
        await mediator.Publish(new TermChanged());      // Invalidate cached
        return newId;
    }
}
