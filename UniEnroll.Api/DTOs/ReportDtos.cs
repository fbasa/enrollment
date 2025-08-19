namespace UniEnroll.Api.DTOs;

public record EnrollmentReportRow(string TermCode, string CourseCode, string Title, int EnrolledCount, int WaitlistedCount);
public record InstructorLoadRow(string TermCode, long InstructorId, string InstructorUserName, int OfferingCount, int EnrolledStudents);
public record RoomUtilizationRow(string TermCode, string RoomCode, int Capacity, int Enrolled, int UtilizationPercent);
