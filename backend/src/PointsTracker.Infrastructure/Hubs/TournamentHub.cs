using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PointsTracker.Application.Tournaments.DTOs;

namespace PointsTracker.Infrastructure.Hubs;

[AllowAnonymous]
public class TournamentHub : Hub
{
    public Task JoinTournament(string tournamentId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");

    public Task LeaveTournament(string tournamentId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament-{tournamentId}");
}

public static class TournamentHubExtensions
{
    public static Task BroadcastTournamentUpdate(this IHubContext<TournamentHub> hub, Guid tournamentId, TournamentDto dto) =>
        hub.Clients.Group($"tournament-{tournamentId}").SendAsync("TournamentUpdated", dto);

    public static Task BroadcastTournamentDeleted(this IHubContext<TournamentHub> hub, Guid tournamentId) =>
        hub.Clients.Group($"tournament-{tournamentId}").SendAsync("TournamentDeleted", tournamentId);
}
