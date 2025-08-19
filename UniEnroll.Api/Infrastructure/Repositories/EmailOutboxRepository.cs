using Dapper;
using System.Data;
using UniEnroll.Api.Infrastructure;
using UniEnroll.Api.Messaging;

namespace UniEnroll.Api.Infrastructure.Repositories;

public interface IEmailOutboxRepository
{
    Task<long> AddPendingAsync(EmailMessage msg, IDbTransaction? tx, CancellationToken ct);
    Task<IReadOnlyList<(long Id, EmailMessage Msg)>> TakePendingAsync(int take, CancellationToken ct);
    Task MarkEnqueuedAsync(long id, CancellationToken ct);
    Task MarkSentAsync(long id, CancellationToken ct);
    Task MarkFailedAsync(long id, string error, CancellationToken ct);
}

public sealed class EmailOutboxRepository(IDbConnectionFactory db) : IEmailOutboxRepository
{
    public async Task<long> AddPendingAsync(EmailMessage msg, IDbTransaction? tx, CancellationToken ct)
    {
        var conn = tx?.Connection ?? await db.CreateOpenConnectionAsync(ct);
        var sql = @"INSERT dbo.EmailOutbox(toEmail,toName,subject,bodyText,bodyHtml,meta)
                    OUTPUT INSERTED.outboxId
                    VALUES(@ToEmail,@ToName,@Subject,@BodyText,@BodyHtml,@Meta)";
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            msg.ToEmail,
            msg.ToName,
            msg.Subject,
            msg.BodyText,
            msg.BodyHtml,
            Meta = System.Text.Json.JsonSerializer.Serialize(msg.Metadata)
        }, tx);
    }

    public async Task<IReadOnlyList<(long, EmailMessage)>> TakePendingAsync(int take, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        // Pseudo-reserve: flip to 'Enqueued' to avoid double pickup
        var tx = conn.BeginTransaction();
        var ids = (await conn.QueryAsync<long>(@"
            SELECT TOP (@take) outboxId FROM dbo.EmailOutbox WITH (READPAST, ROWLOCK)
            WHERE status='Pending' ORDER BY createdAtUtc", new { take }, tx)).ToArray();

        if (ids.Length == 0) { tx.Commit(); return Array.Empty<(long, EmailMessage)>(); }

        await conn.ExecuteAsync(@"UPDATE dbo.EmailOutbox SET status='Enqueued' WHERE outboxId IN @ids", new { ids }, tx);

        var rows = (await conn.QueryAsync(@"
            SELECT outboxId, toEmail, toName, subject, bodyText, bodyHtml, meta
            FROM dbo.EmailOutbox WHERE outboxId IN @ids", new { ids }, tx)).ToList();

        tx.Commit();

        return rows.Select(r => ((long)r.outboxId,
            new EmailMessage((string)r.toEmail, (string?)r.toName, (string)r.subject, (string?)r.bodyText, (string?)r.bodyHtml,
                string.IsNullOrWhiteSpace((string?)r.meta) ? null :
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>((string)r.meta))))
            .ToList();
    }

    public async Task MarkEnqueuedAsync(long id, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE dbo.EmailOutbox SET status='Enqueued' WHERE outboxId=@id", new { id });
    }

    public async Task MarkSentAsync(long id, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE dbo.EmailOutbox SET status='Sent' WHERE outboxId=@id", new { id });
    }

    public async Task MarkFailedAsync(long id, string error, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE dbo.EmailOutbox SET status='Failed', attemptCount=attemptCount+1, lastError=@error WHERE outboxId=@id",
            new { id, error = error[..Math.Min(error.Length, 1000)] });
    }
}
