namespace UniEnroll.Api.Realtime;

// Server - Client contract (method names are what clients subscribe to)
public interface IEnrollmentClient
{
    Task EnrollmentChanged(EnrollmentEventDto evt);
    Task OfferingSeatCounts(OfferingSeatCountsDto counts);
}
