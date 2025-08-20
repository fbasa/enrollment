namespace UniEnroll.Domain.Response;

public record InstructorLoadRow(string TermCode, long InstructorId, string InstructorUserName, int OfferingCount, int EnrolledStudents);
