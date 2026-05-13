using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PointsTracker.Application.Counters.DTOs;

namespace PointsTracker.Infrastructure.Hubs;

[AllowAnonymous]
public class CounterHub : Hub
{
    public async Task JoinCounter(string counterId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"counter-{counterId}");
    }

    public async Task LeaveCounter(string counterId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"counter-{counterId}");
    }

    /// <summary>
    /// Join the per-user group used by the dashboard to receive updates for
    /// every counter the user owns. No userId param — the server uses the
    /// authenticated identity, which removes any risk of snooping on
    /// another user's activity. UserIdentifier is wired (see
    /// <see cref="PtsIdUserIdProvider"/>) to the internal pts_id so it lines
    /// up with the DTO's OwnerUserId.
    /// </summary>
    public async Task JoinMyUpdates()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    public async Task LeaveMyUpdates()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
}

public static class CounterHubExtensions
{
    public static async Task BroadcastScoreUpdate(
        this IHubContext<CounterHub> hub,
        Guid counterId,
        CounterDto dto)
    {
        // Counter group: every viewer of this specific counter.
        await hub.Clients.Group($"counter-{counterId}").SendAsync("ScoreUpdated", dto);

        // User group: the owner's dashboard. Skipped for anonymous counters.
        if (dto.OwnerUserId.HasValue)
            await hub.Clients.Group($"user-{dto.OwnerUserId}").SendAsync("ScoreUpdated", dto);
    }

    /// <summary>
    /// Notify subscribers that a counter has been removed so the dashboard
    /// can drop it from the list and viewers can be bounced off the page.
    /// </summary>
    public static async Task BroadcastCounterDeleted(
        this IHubContext<CounterHub> hub,
        Guid counterId,
        Guid? ownerUserId)
    {
        await hub.Clients.Group($"counter-{counterId}").SendAsync("CounterDeleted", counterId);
        if (ownerUserId.HasValue)
            await hub.Clients.Group($"user-{ownerUserId}").SendAsync("CounterDeleted", counterId);
    }
}
