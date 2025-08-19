using MediatR;

namespace UniEnroll.Api.Realtime;

public sealed record EnrollmentCommitted(
    EnrollmentEventDto Event,
    OfferingSeatCountsDto SeatCounts
) : INotification;
