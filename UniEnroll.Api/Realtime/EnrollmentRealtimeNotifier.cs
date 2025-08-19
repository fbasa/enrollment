using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace UniEnroll.Api.Realtime;

public sealed class EnrollmentRealtimeNotifier(
    IHubContext<EnrollmentHub, IEnrollmentClient> hub,
    ILogger<EnrollmentRealtimeNotifier> log
) : INotificationHandler<EnrollmentCommitted>
{
    public async Task Handle(EnrollmentCommitted n, CancellationToken ct)
    {
        // broadcast to: whole term, specific offering, and the specific student
        var termGroup = HubGroupNames.Term(n.Event.TermId);
        var offeringGroup = HubGroupNames.Offering(n.Event.OfferingId);
        var studentGroup = HubGroupNames.Student(n.Event.StudentId);

        await Task.WhenAll(
            hub.Clients.Group(termGroup).EnrollmentChanged(n.Event),
            hub.Clients.Group(offeringGroup).EnrollmentChanged(n.Event),
            hub.Clients.Group(studentGroup).EnrollmentChanged(n.Event),
            hub.Clients.Group(offeringGroup).OfferingSeatCounts(n.SeatCounts)
        );

        log.LogInformation("Realtime: pushed enrollment {EnrollmentId} ({Status}) to term/offering/student groups",
            n.Event.EnrollmentId, n.Event.Status);
    }
}
