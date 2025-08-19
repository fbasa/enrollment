namespace UniEnroll.Api.Errors;

public sealed class ConflictException(string message, string code = ErrorCodes.ConcurrencyConflict)
    : DomainException(message, code, 409);
