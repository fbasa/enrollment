using MediatR;
using UniEnroll.Api.DTOs;
using UniEnroll.Api.Infrastructure.Repositories;

namespace UniEnroll.Api.Application.Courses.Queries;

public record ListCoursesQuery(string? Search, long? DepartmentId, int Page, int PageSize) : IRequest<PageResult<CourseDto>>;

public sealed class ListCoursesHandler(ICoursesRepository repo) : IRequestHandler<ListCoursesQuery, PageResult<CourseDto>>
{
    public Task<PageResult<CourseDto>> Handle(ListCoursesQuery q, CancellationToken ct) => repo.ListAsync(q.Search, q.DepartmentId, q.Page, q.PageSize, ct);
}

