using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace UniEnroll.Api.Realtime;

// Optional: client - server; keep minimal & validated
[Authorize] // reuse your JWT; hub inherits normal authz
public sealed class EnrollmentHub : Hub<IEnrollmentClient>
{
    // Allow a client to subscribe to a term-wide channel (validated server-side)
    public async Task SubscribeTerm(long termId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Term(termId));

    public async Task UnsubscribeTerm(long termId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Term(termId));

    // Private groups (server joins clients automatically on connect if desired)
    internal static class GroupNames
    {
        public static string Term(long termId) => $"term:{termId}";
        public static string Student(long userId) => $"student:{userId}";
        public static string Offering(long id) => $"offering:{id}";
    }

    // Example: auto-join a per-user group so a student gets their own events
    public override async Task OnConnectedAsync()
    {
        // If you have CurrentUserAccessor, resolve DB userId here and join student group
        await base.OnConnectedAsync();
    }
}
