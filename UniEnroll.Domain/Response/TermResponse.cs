namespace UniEnroll.Domain.Response;

public sealed class TermResponse
{
    public long TermId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime AddDropDeadlineDate { get; set; }
}
