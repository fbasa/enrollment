namespace UniEnroll.Api.DTOs;

public record PaymentDto(long PaymentId, long InvoiceId, decimal Amount, DateTime PaidAtUtc, string Method);
public record PaymentRequest(decimal Amount, DateTime? PaidAtUtc, string Method);
