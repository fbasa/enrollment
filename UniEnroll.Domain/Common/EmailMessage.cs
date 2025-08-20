namespace UniEnroll.Domain.Common;

public sealed record EmailMessage(
    string ToEmail,
    string? ToName,
    string Subject,
    string? BodyText,
    string? BodyHtml,
    IReadOnlyDictionary<string, object?>? Metadata = null);
