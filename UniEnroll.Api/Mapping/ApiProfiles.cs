using AutoMapper;

namespace UniEnroll.Api.Mapping;

// Present for future use; current endpoints project directly from SQL to DTOs.
public sealed class ApiProfiles : Profile
{
    public ApiProfiles()
    {
        // Example:
        // CreateMap<DbCourseRow, CourseDto>();
    }
}
