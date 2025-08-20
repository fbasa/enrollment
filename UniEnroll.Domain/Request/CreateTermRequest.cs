namespace UniEnroll.Domain.Request;

public sealed record CreateTermRequest(string Code, DateOnly StartDate, DateOnly EndDate, DateOnly AddDropDeadlineDate);