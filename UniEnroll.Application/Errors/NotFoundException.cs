namespace UniEnroll.Application.Errors;

public sealed class NotFoundException(string message)
    : DomainException(message, ErrorCodes.NotFound, 404);
