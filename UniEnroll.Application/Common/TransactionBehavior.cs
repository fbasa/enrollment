using MediatR;
using UniEnroll.Infrastructure.Transactions;
using UniEnroll.Infrastructure;

namespace UniEnroll.Application.Common;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IDbConnectionFactory factory,
    IDbSession session) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Only wrap requests that opt-in
        if (request is not ITransactionalRequest)
            return await next();

        await using var uow = await UnitOfWork.BeginAsync(factory, ct);
        session.Use(uow.Connection, uow.Transaction);

        try
        {
            var resp = await next();
            await uow.CommitAsync(ct);
            return resp;
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
        finally
        {
            session.Clear();
        }
    }
}
