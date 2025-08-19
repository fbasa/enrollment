namespace UniEnroll.Api.Realtime;

public static class HubGroupNames
{
    public static string Term(long termId) => $"term:{termId}";
    public static string Offering(long offeringId) => $"offering:{offeringId}";
    public static string Student(long studentId) => $"student:{studentId}";
}
