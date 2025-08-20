namespace UniEnroll.Domain.Request;

public record OfferingUpsertRequest(
    long TermId,
    long CourseId,
    string Section,
    long RoomId,
    int Capacity,
    int WaitlistCapacity,
    IReadOnlyList<long> TimeSlotIds);
