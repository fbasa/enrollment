namespace UniEnroll.Api.Common.Idempotency;

public sealed class IdempotencyOptions
{
    /// Header to read the idempotency key from (default: X-Idempotency-Key)
    public string HeaderName { get; init; } = "X-Idempotency-Key";

    /// How long to keep completed responses
    public TimeSpan StoreTtl { get; init; } = TimeSpan.FromMinutes(10);

    /// How long to hold in-flight lock (protects duplicate concurrent calls)
    public TimeSpan LockTtl { get; init; } = TimeSpan.FromSeconds(30);

    /// How long a second caller waits for the first to finish (polling)
    public TimeSpan WaitForExisting { get; init; } = TimeSpan.FromSeconds(5);
}
