using MediatR;
using UniEnroll.Api.Common;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Payments.Commands;

public record CreateInvoiceCommand(CreateInvoiceRequest Request) : IRequest<long>, ITransactionalRequest;

public sealed class CreateInvoiceHandler(IPaymentsRepository repo)
    : IRequestHandler<CreateInvoiceCommand, long>
{
    public Task<long> Handle(CreateInvoiceCommand cmd, CancellationToken ct)
        => repo.CreateInvoiceAsync(cmd.Request, ct);
}
