using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Response;
using UniEnroll.Application.Caching;

namespace UniEnroll.Application.Handlers.Queries;

public record ListCoursesQuery(string? Search, long? DepartmentId, int Page, int PageSize) : IRequest<PageResult<CourseResponse>>, ICacheableQuery
{
    public string CacheKey => $"courses:search:t{Search}:d{DepartmentId}:p{Page}:s{PageSize}";

    public TimeSpan? Ttl => TimeSpan.FromSeconds(30);
}

public sealed class ListCoursesHandler(ICoursesRepository repo) : IRequestHandler<ListCoursesQuery, PageResult<CourseResponse>>
{
    public Task<PageResult<CourseResponse>> Handle(ListCoursesQuery q, CancellationToken ct) => repo.ListAsync(q.Search, q.DepartmentId, q.Page, q.PageSize, ct);
}

