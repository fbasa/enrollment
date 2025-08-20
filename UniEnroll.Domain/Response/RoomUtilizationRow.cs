namespace UniEnroll.Domain.Response;

public record RoomUtilizationRow(string TermCode, string RoomCode, int Capacity, int Enrolled, int UtilizationPercent);
