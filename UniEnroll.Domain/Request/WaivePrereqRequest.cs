namespace UniEnroll.Domain.Request;

public record WaivePrereqRequest(long StudentId, long OfferingId, string Reason);
