namespace UniEnroll.Api.DTOs;
//public sealed record TermDto(long TermId, string Code, DateOnly StartDate, DateOnly EndDate, DateOnly AddDropDeadlineDate);
public sealed class TermDto
{
    public long TermId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime AddDropDeadlineDate { get; set; }
}
public sealed record CreateTermRequest(string Code, DateOnly StartDate, DateOnly EndDate, DateOnly AddDropDeadlineDate);