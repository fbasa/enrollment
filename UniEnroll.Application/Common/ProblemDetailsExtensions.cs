using Microsoft.AspNetCore.Http;

namespace UniEnroll.Application.Common;

public static class ProblemDetailsExtensions
{
    public static IResult DomainProblem(string title, string detail, int status = StatusCodes.Status422UnprocessableEntity, string? type = null)
        => Results.Problem(title: title, detail: detail, statusCode: status, type: type ?? "about:blank");
}

