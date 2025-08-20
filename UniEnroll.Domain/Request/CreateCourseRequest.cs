namespace UniEnroll.Domain.Request;

public record CreateCourseRequest(string Code, string Title, int Units, string DepartureCode);
