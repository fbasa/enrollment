namespace UniEnroll.Domain.Response;

public record InvoiceResponse(long InvoiceId, long StudentId, long TermId, decimal Amount, string Status, DateTime CreatedAtUtc);
