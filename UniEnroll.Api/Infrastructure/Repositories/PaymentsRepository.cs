using Dapper;
using UniEnroll.Api.Infrastructure.Transactions;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Request;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Infrastructure.Repositories;

public interface IPaymentsRepository
{
    Task<PageResult<InvoiceResponse>> ListInvoicesAsync(long? studentId, long? termId, int page, int pageSize, CancellationToken ct);
    Task<long> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct);
    Task AddPaymentAsync(long invoiceId, decimal amount, DateTime paidAtUtc, string method, CancellationToken ct);
    Task<IEnumerable<dynamic>> FinanceRowsAsync(long? termId, CancellationToken ct);
}

public sealed class PaymentsRepository(IDbConnectionFactory db, IDbSession session) : IPaymentsRepository
{
    public async Task<PageResult<InvoiceResponse>> ListInvoicesAsync(long? studentId, long? termId, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var p = new DynamicParameters(new { studentId, termId, skip = (page - 1) * pageSize, take = pageSize });
        using var multi = await conn.QueryMultipleAsync(SqlTemplates.ListInvoicesPaged, p);
        var items = (await multi.ReadAsync<InvoiceResponse>()).ToList();
        var total = await multi.ReadFirstAsync<int>();
        return new PageResult<InvoiceResponse>(items, total);
    }

    public async Task<long> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(SqlTemplates.CreateInvoice, request, session.Transaction);
    }

    public async Task AddPaymentAsync(long invoiceId, decimal amount, DateTime paidAtUtc, string method, CancellationToken ct)
    {
        var conn = session.Connection ?? await db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(SqlTemplates.AddPayment, new { InvoiceId = invoiceId, Amount = amount, PaidAtUtc = paidAtUtc, Method = method }, session.Transaction);
    }

    public async Task<IEnumerable<dynamic>> FinanceRowsAsync(long? termId, CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        return await conn.QueryAsync(SqlTemplates.FinanceCsv, new { termId });
    }
}
