namespace UniEnroll.Api.Messaging;

public sealed class SendGridOptions
{
    public string ApiKey { get; init; } = "";           // required (use env/KeyVault)
    public string FromEmail { get; init; } = "noreply@unienroll.local";
    public string FromName { get; init; } = "UniEnroll";
    public bool SandboxMode { get; init; } = false;    // true in dev to avoid real sends
    public string[] DefaultCategories { get; init; } = new[] { "unienroll" };
}
