using MediatR;
using UniEnroll.Application.Caching.CacheInvalidator;
using UniEnroll.Domain.Request;
using UniEnroll.Infrastructure.Repositories;

namespace UniEnroll.Application.Handlers.Commands;

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
