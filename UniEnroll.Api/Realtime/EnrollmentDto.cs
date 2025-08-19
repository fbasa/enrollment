namespace UniEnroll.Api.Realtime;

public sealed record EnrollmentEventDto(
    long EnrollmentId,
    long OfferingId,
    long TermId,
    long StudentId,
    string Status,          // Enrolled | Waitlisted | Dropped
    DateTime OccurredAtUtc  // store UTC, render local in UI
);

public sealed record OfferingSeatCountsDto(
    long OfferingId,
    int Enrolled,
    int Capacity,
    int Waitlisted,
    int WaitlistCapacity
);
