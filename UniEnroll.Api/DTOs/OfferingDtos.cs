namespace UniEnroll.Api.DTOs;

public record OfferingListItemDto(
    long OfferingId,
    string TermCode,
    string CourseCode,
    string Section,
    string RoomCode,
    int Capacity,
    int Enrolled,
    int Waitlisted);

public record OfferingDetailDto(
    long OfferingId,
    string TermCode,
    string CourseCode,
    string Title,
    string Section,
    string RoomCode,
    int Capacity,
    int WaitlistCapacity,
    IReadOnlyList<ScheduleSlotDto> Schedule,
    string ETag);

public record ScheduleSlotDto(int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public record OfferingUpsertRequest(
    long TermId,
    long CourseId,
    string Section,
    long RoomId,
    int Capacity,
    int WaitlistCapacity,
    IReadOnlyList<long> TimeSlotIds);
