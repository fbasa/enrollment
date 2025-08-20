namespace UniEnroll.Domain.Response;

public record ScheduleSlotResponse(int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
