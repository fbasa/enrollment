namespace UniEnroll.Domain.Request;

public record CreateInvoiceRequest(long StudentId, long TermId, decimal Amount);
