using MediatR;
using UniEnroll.Infrastructure.Repositories;
using UniEnroll.Domain.Common;
using UniEnroll.Domain.Response;

namespace UniEnroll.Api.Application.Courses.Queries;

public record ListCoursesQuery(string? Search, long? DepartmentId, int Page, int PageSize) : IRequest<PageResult<CourseResponse>>;

public sealed class ListCoursesHandler(ICoursesRepository repo) : IRequestHandler<ListCoursesQuery, PageResult<CourseResponse>>
{
    public Task<PageResult<CourseResponse>> Handle(ListCoursesQuery q, CancellationToken ct) => repo.ListAsync(q.Search, q.DepartmentId, q.Page, q.PageSize, ct);
}

