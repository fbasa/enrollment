using MediatR;
using UniEnroll.Api.Common;
using UniEnroll.Api.Common.Idempotency;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Request;

namespace UniEnroll.Api.Application.Payments.Commands;

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
