namespace UniEnroll.Domain.Response;

public record EnrollmentReportRow(string TermCode, string CourseCode, string Title, int EnrolledCount, int WaitlistedCount);
