namespace UniEnroll.Domain.Response;

public record CourseResponse(long CourseId, string Code, string Title, int Units, string DepartmentCode);
