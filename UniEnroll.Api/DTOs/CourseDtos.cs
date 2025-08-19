namespace UniEnroll.Api.DTOs;
public record CourseDto(long CourseId, string Code, string Title, int Units, string DepartmentCode);
public record CreateCourseRequest(string Code, string Title, int Units, string DepartureCode);
