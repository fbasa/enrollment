namespace UniEnroll.Domain.Request;

public record PaymentRequest(decimal Amount, DateTime? PaidAtUtc, string Method);
