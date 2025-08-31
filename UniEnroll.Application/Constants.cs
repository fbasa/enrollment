namespace UniEnroll.Application;

public static class Headers
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string IdempotencyKey = "Idempotency-Key";
    public const string IfMatch = "If-Match";

    public const string CorrelationLogKey = "correlation.id";
}
