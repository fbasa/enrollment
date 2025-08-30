using MediatR;
using UniEnroll.Application.Common.Idempotency;
using UniEnroll.Domain.Request;
using UniEnroll.Infrastructure.Repositories;

namespace UniEnroll.Application.Handlers.Commands;

public record PayInvoiceCommand(long InvoiceId, PaymentRequest Request) : IRequest<Unit>, ITransactionalRequest, IIdempotentRequest;

public sealed class PayInvoiceHandler(IPaymentsRepository repo)
    : IRequestHandler<PayInvoiceCommand, Unit>
{
    public async Task<Unit> Handle(PayInvoiceCommand cmd, CancellationToken ct)
    {
        var when = cmd.Request.PaidAtUtc ?? DateTime.UtcNow;
        await repo.AddPaymentAsync(cmd.InvoiceId, cmd.Request.Amount, when, cmd.Request.Method, ct);
        return Unit.Value;
    }
}
