namespace UniEnroll.Domain.Response;

public record OfferingListItemResponse(
    long OfferingId,
    string TermCode,
    string CourseCode,
    string Section,
    string RoomCode,
    int Capacity,
    int Enrolled,
    int Waitlisted);
