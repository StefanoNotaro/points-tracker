using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Interfaces;
using System.Security.Claims;

namespace PointsTracker.Infrastructure.Hubs;

[AllowAnonymous]
public class CounterHub(
    ICounterRepository counterRepository,
    ICounterAuthorizationService authorizationService,
    ILogger<CounterHub> logger) : Hub
{
    public async Task JoinCounter(string counterId, string? sessionToken = null, string? shareToken = null)
    {
        if (!Guid.TryParse(counterId, out var parsedCounterId))
            throw new HubException("counter.liveAccessDenied");

        var counter = await counterRepository.GetByIdAsync(parsedCounterId, Context.ConnectionAborted);
        if (counter is null)
            throw new HubException("counter.liveAccessDenied");

        var userId = GetUserId();
        var isSuperAdmin = HasRole("super_admin");
        var access = authorizationService.GetLiveAccess(counter, userId, sessionToken, shareToken, isSuperAdmin);
        if (!access.CanRead)
        {
            logger.LogWarning(
                "Denied SignalR counter join for counter {CounterId}. ConnectionId={ConnectionId}, UserId={UserId}",
                counterId,
                Context.ConnectionId,
                userId);
            throw new HubException("counter.liveAccessDenied");
        }

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

    private Guid? GetUserId()
    {
        var value = Context.User?.FindFirstValue("pts_id");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private bool HasRole(string role)
    {
        var hierarchy = new[] { "user", "admin", "super_admin" };
        var userRole = Context.User?.FindFirstValue("pts_role") ?? "user";
        return Array.IndexOf(hierarchy, userRole) >= Array.IndexOf(hierarchy, role);
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
