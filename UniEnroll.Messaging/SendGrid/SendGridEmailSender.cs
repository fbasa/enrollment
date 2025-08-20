using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.Json;
using UniEnroll.Domain.Common;
using UniEnroll.Messaging.Abstractions;

namespace UniEnroll.Messaging.SendGrid;

public sealed class SendGridEmailSender(
    SendGridClient client,
    IOptions<SendGridOptions> options,
    ILogger<SendGridEmailSender> log) : IEmailSender
{
    private readonly SendGridOptions _o = options.Value;

    // Simple, robust retry on 429/5xx
    private static readonly AsyncRetryPolicy<Response> _retry = Policy
        .HandleResult<Response>(r => (int)r.StatusCode is 429 or >= 500)
        .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)),
            (res, _, attempt, _) =>
            {
                var status = (int)res.Result.StatusCode;
                // no PII/key in logs
                Console.WriteLine($"SendGrid retry {attempt}, status={status}");
            });

    public async Task SendAsync(EmailMessage msg, CancellationToken ct)
    {
        var from = new EmailAddress(_o.FromEmail, _o.FromName);
        var to = new EmailAddress(msg.ToEmail, msg.ToName);

        var sg = MailHelper.CreateSingleEmail(from, to, msg.Subject, msg.BodyText, msg.BodyHtml);

        // Categories
        foreach (var c in _o.DefaultCategories) sg.AddCategory(c);
        if (msg.Metadata is { Count: > 0 } meta)
        {
            // Optional: pass metadata as custom args (string values)
            foreach (var kv in meta)
                sg.AddCustomArg($"meta_{kv.Key}", JsonSerializer.Serialize(kv.Value));
        }

        // Sandbox mode (no real email sent)
        if (_o.SandboxMode)
            sg.MailSettings = new MailSettings { SandboxMode = new SandboxMode { Enable = true } };

        // Execute with retry
        var response = await _retry.ExecuteAsync(_ => client.SendEmailAsync(sg, ct), CancellationToken.None);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            log.LogError("SendGrid failed: {Status} {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"SendGrid send failed with {(int)response.StatusCode}");
        }

        log.LogInformation("Email sent to {To}", msg.ToEmail);
    }
}