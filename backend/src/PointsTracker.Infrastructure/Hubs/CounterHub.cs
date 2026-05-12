using Microsoft.AspNetCore.SignalR;
using PointsTracker.Application.Counters.DTOs;

namespace PointsTracker.Infrastructure.Hubs;

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
}

public static class CounterHubExtensions
{
    public static Task BroadcastScoreUpdate(
        this IHubContext<CounterHub> hub,
        Guid counterId,
        CounterDto dto) =>
        hub.Clients.Group($"counter-{counterId}").SendAsync("ScoreUpdated", dto);
}
