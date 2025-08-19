namespace UniEnroll.Api.DTOs;

public record EnrollRequest(long StudentId, long OfferingId);
public record EnrollResponse(long EnrollmentId, string Status);
public record DropRequest(string Reason);
public record WaivePrereqRequest(long StudentId, long OfferingId, string Reason);
public record CapacityOverrideRequest(int NewCapacity, string Reason);
