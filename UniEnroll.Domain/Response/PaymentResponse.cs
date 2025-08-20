namespace UniEnroll.Domain.Response;

public record PaymentResponse(long PaymentId, long InvoiceId, decimal Amount, DateTime PaidAtUtc, string Method);
