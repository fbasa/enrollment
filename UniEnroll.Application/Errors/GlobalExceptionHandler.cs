using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Serilog;                         // <— Serilog
using System.Text.Json;

namespace UniEnroll.Application.Errors;

public sealed class ProblemDetailsException : Exception
{
    public int Status { get; }
    public string? Detail { get; }
    public ProblemDetailsException(int status, string title, string? detail = null)
        : base(title) { Status = status; Detail = detail; }
}

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var correlationId = http.Response.Headers.TryGetValue(Headers.CorrelationId, out var cid)
            ? cid.ToString()
            : Guid.NewGuid().ToString();

        ProblemDetails problem = ex switch
        {
            ValidationException fv => new ProblemDetails
            {
                Title = "Validation failed",
                Status = 422,
                Type = $"https://errors.example/{ErrorCodes.ValidationFailed}",
                Detail = "One or more validation errors occurred.",
                Instance = http.Request.Path,
                Extensions = { ["errors"] = fv.Errors }
                    //.GroupBy(e => e.PropertyName)
                    //.ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) }
            },

            DomainException de => new ProblemDetails
            {
                Title = de.Message,
                Status = de.Status,
                Type = $"https://errors.example/{de.Code}",
                Detail = de.InnerException?.Message,
                Instance = http.Request.Path
            },

            SqlException sqlEx => CreateFromSql(sqlEx, http),

            UnauthorizedAccessException => new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://errors.example/unauthorized",
                Instance = http.Request.Path
            },

            _ => new ProblemDetails
            {
                Title = "Unexpected error",
                Status = StatusCodes.Status500InternalServerError,
                Type = $"https://errors.example/{ErrorCodes.Unknown}",
                Detail = ex.Message,
                Instance = http.Request.Path
            }
        };

        // Correlation & trace id on every problem
        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["traceId"] = http.TraceIdentifier;

        // ---- Serilog: structured error log with useful context ----
        Log.ForContext("CorrelationId", correlationId)
           .ForContext("TraceId", http.TraceIdentifier)
           .ForContext("Path", http.Request.Path.Value)
           .ForContext("Method", http.Request.Method)
           .ForContext("UserName", http.User?.Identity?.Name ?? "anon")
           .ForContext("Problem", problem, destructureObjects: true)
           .Error(ex, "Unhandled exception -> ProblemDetails {Status} {Type}", problem.Status, problem.Type);

        http.Response.StatusCode = problem.Status ?? 500;
        http.Response.ContentType = "application/problem+json";
        await http.Response.WriteAsync(JsonSerializer.Serialize(problem), ct);
        return true;

        static ProblemDetails CreateFromSql(SqlException sqlEx, HttpContext http)
        {
            var (status, code, title) = SqlErrorClassifier.Classify(sqlEx);
            return new ProblemDetails
            {
                Title = title,
                Status = status,
                Type = $"https://errors.example/{code}",
                Detail = sqlEx.Message,
                Instance = http.Request.Path
            };
        }
    }
}
