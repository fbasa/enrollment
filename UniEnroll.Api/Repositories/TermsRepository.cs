using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using UniEnroll.Api.DTOs;

namespace UniEnroll.Api.Infrastructure.Repositories;

public interface ITermsRepository
{
    Task<long> CreateAsync(CreateTermRequest request, CancellationToken ct);
    Task<IReadOnlyList<TermDto>> GetAllAsync(CancellationToken ct);
}

public sealed class TermsRepository(IDbConnectionFactory db) : ITermsRepository
{
    public async Task<long> CreateAsync(CreateTermRequest req, CancellationToken ct)
    {
        var conn = await db.CreateOpenConnectionAsync(ct);
    
        var termId = await conn.ExecuteScalarAsync<long>(
            """
            INSERT [dbo].[Term]
                    ([code]
                    ,[startDate]
                    ,[endDate]
                    ,[addDropDeadlineDate])
            VALUES
                    (
                    @Code
                    ,@StartDate
                    ,@EndDate
                    ,@AddDropDeadlineDate
                    )
            SELECT SCOPE_IDENTITY();
            """, req);

        return termId;
   
    }

    public async Task<IReadOnlyList<TermDto>> GetAllAsync(CancellationToken ct)
    {
        await using var conn = await db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<TermDto>(SqlTemplates.ListTerms);
        return rows.ToList();
    }
}
