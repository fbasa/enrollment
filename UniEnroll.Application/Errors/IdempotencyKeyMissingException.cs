using Microsoft.AspNetCore.Http;

namespace UniEnroll.Application.Errors;

public sealed class IdempotencyKeyMissingException(string message="") 
    : DomainException(
        message ?? "Missing X-Idempotency-Key", 
        ErrorCodes.IdempotencyKeyMissing,
        StatusCodes.Status400BadRequest, 
        new Exception("This operation requires X-Idempotency-Key. Provide a unique key per retry.")
        );
