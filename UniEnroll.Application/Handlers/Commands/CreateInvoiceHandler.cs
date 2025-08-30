using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Request;
using UniEnroll.Application.Common.Idempotency;

namespace UniEnroll.Application.Handlers.Commands;

public record CreateInvoiceCommand(CreateInvoiceRequest Request) : IRequest<long>, ITransactionalRequest, IIdempotentRequest;

public sealed class CreateInvoiceHandler(IPaymentsRepository repo)
    : IRequestHandler<CreateInvoiceCommand, long>
{
    public Task<long> Handle(CreateInvoiceCommand cmd, CancellationToken ct)
        => repo.CreateInvoiceAsync(cmd.Request, ct);
}
