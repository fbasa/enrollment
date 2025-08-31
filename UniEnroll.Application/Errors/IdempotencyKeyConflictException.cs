using Microsoft.AspNetCore.Http;

namespace UniEnroll.Application.Errors;

public sealed class IdempotencyKeyConflictException(string message="")
    : DomainException(
        message ?? "Idempotency key conflict", 
        ErrorCodes.IdempotencyKeyConflict,
        StatusCodes.Status400BadRequest, 
        new Exception("The X-Idempotency-Key has already been used with different request data.")
        );