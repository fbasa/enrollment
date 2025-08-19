namespace UniEnroll.Api.Messaging;

public static class EnrollmentEmailComposer
{
    public static EmailMessage Enrolled(string toEmail, string? toName, long offeringId, long termId)
        => new(toEmail, toName, "Enrollment Confirmed",
            BodyText: $"You are enrolled. Offering {offeringId}.",
            BodyHtml: $"<p>You are <b>enrolled</b> in offering <strong>{offeringId}</strong>.</p>",
            Metadata: new Dictionary<string, object?> { ["offeringId"] = offeringId, ["termId"] = termId });

    public static EmailMessage Waitlisted(string toEmail, string? toName, long offeringId, long termId)
        => new(toEmail, toName, "Added to Waitlist",
            BodyText: $"You are waitlisted. Offering {offeringId}.",
            BodyHtml: $"<p>You are <b>waitlisted</b>...</p>",
            new Dictionary<string, object?> { ["offeringId"] = offeringId, ["termId"] = termId });
}
