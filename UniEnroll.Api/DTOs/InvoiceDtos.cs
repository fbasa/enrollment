namespace UniEnroll.Api.DTOs;

public record InvoiceDto(long InvoiceId, long StudentId, long TermId, decimal Amount, string Status, DateTime CreatedAtUtc);
public record CreateInvoiceRequest(long StudentId, long TermId, decimal Amount);
