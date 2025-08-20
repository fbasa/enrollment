namespace UniEnroll.Domain.Response;

public record OfferingDetailResponse(
    long OfferingId,
    string TermCode,
    string CourseCode,
    string Title,
    string Section,
    string RoomCode,
    int Capacity,
    int WaitlistCapacity,
    IReadOnlyList<ScheduleSlotResponse> Schedule,
    string ETag);
