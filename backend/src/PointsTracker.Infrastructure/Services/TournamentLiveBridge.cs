using Microsoft.AspNetCore.SignalR;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Hubs;

namespace PointsTracker.Infrastructure.Services;

public class TournamentLiveBridge(
    ITournamentRepository tournaments,
    ICounterRepository counters,
    ITournamentMapper mapper,
    IHubContext<TournamentHub> hub) : ITournamentLiveBridge
{
    public async Task OnCounterChangedAsync(Guid counterId, CancellationToken ct = default)
    {
        var t = await tournaments.GetByLinkedCounterAsync(counterId, ct);
        if (t is null) return;

        // Reconcile if the linked counter has just finished but the match
        // hasn't been recorded yet. Done eagerly here so the bracket reflects
        // the result in real time instead of waiting for the next GET.
        var match = t.Matches.FirstOrDefault(m => m.CounterId == counterId);
        if (match?.Status == TournamentMatchStatus.InProgress)
        {
            var counter = await counters.GetByIdAsync(counterId, ct);
            if (counter?.Status == CounterStatus.Finished)
            {
                var winnerId = counter.SetsWonA > counter.SetsWonB
                    ? match.ParticipantAId
                    : match.ParticipantBId;
                if (winnerId is not null)
                {
                    t.RecordMatchResult(match.Id, winnerId.Value);
                    await tournaments.SaveChangesAsync(ct);
                }
            }
        }

        var dto = mapper.ToDto(t, t.OwnerUserId, null);
        await hub.BroadcastTournamentUpdate(t.Id, dto);
    }
}
