using MediatR;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Caching.CacheInvalidator;
using UniEnroll.Api.Infrastructure.Repositories;

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
