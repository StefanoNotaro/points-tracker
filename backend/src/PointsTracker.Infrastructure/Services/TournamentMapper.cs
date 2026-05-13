using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Services;

public class TournamentMapper(ITournamentAuthorizationService authService) : ITournamentMapper
{
    public TournamentDto ToDto(Tournament t, Guid? actorUserId, string? sessionToken)
    {
        var access = authService.GetAccess(t, actorUserId, sessionToken);
        var participants = t.Participants.ToDictionary(p => p.Id);

        return new TournamentDto(
            t.Id,
            t.Name,
            t.SportType.ToString().ToLowerInvariant(),
            t.Format.ToString().ToLowerInvariant(),
            t.Status.ToString().ToLowerInvariant(),
            t.OwnerUserId,
            access.IsOwner,
            access.CanEdit,
            t.CreatedAt,
            t.UpdatedAt,
            t.StartsAt,
            t.EndsAt,
            new TournamentRulesDto(
                t.CustomPointsPerSet,
                t.CustomLastSetPoints,
                t.CustomSetsToWin,
                t.CustomTotalSets,
                t.CustomWinByTwo,
                t.IndoorSwitchEverySets,
                t.BeachAutoSwitchSides,
                t.CustomTimeoutsPerSet,
                t.CustomTimeoutDurationSeconds,
                t.GroupCount,
                t.AdvancePerGroup,
                HasAnyFinalRule(t)
                    ? new StageRulesDto(t.FinalPointsPerSet, t.FinalLastSetPoints, t.FinalSetsToWin,
                        t.FinalTotalSets, t.FinalWinByTwo, t.FinalTimeoutsPerSet, t.FinalTimeoutDurationSeconds)
                    : null,
                HasAnySemifinalRule(t)
                    ? new StageRulesDto(t.SemifinalPointsPerSet, t.SemifinalLastSetPoints, t.SemifinalSetsToWin,
                        t.SemifinalTotalSets, t.SemifinalWinByTwo, t.SemifinalTimeoutsPerSet, t.SemifinalTimeoutDurationSeconds)
                    : null),
            t.Participants
                .OrderBy(p => p.Seed ?? int.MaxValue)
                .ThenBy(p => p.RegisteredAt)
                .Select(p => new TournamentParticipantDto(p.Id, p.TeamName, p.Seed, p.UserId))
                .ToList(),
            t.Matches
                .OrderBy(m => m.BracketSide)
                .ThenBy(m => m.RoundNumber)
                .ThenBy(m => m.MatchNumber)
                .Select(m => new TournamentMatchDto(
                    m.Id,
                    m.BracketSide.ToString().ToLowerInvariant(),
                    m.RoundNumber,
                    m.MatchNumber,
                    m.GroupNumber,
                    m.ParticipantAId,
                    m.ParticipantAId.HasValue && participants.TryGetValue(m.ParticipantAId.Value, out var pa) ? pa.TeamName : null,
                    m.ParticipantBId,
                    m.ParticipantBId.HasValue && participants.TryGetValue(m.ParticipantBId.Value, out var pb) ? pb.TeamName : null,
                    m.CounterId,
                    m.WinnerParticipantId,
                    m.Status.ToString().ToLowerInvariant(),
                    m.NextMatchId,
                    m.NextLoserMatchId,
                    m.ScheduledAt))
                .ToList(),
            ComputeStandings(t)
        );
    }

    private static bool HasAnyFinalRule(Tournament t) =>
        t.FinalPointsPerSet.HasValue || t.FinalLastSetPoints.HasValue || t.FinalSetsToWin.HasValue
        || t.FinalTotalSets.HasValue || t.FinalWinByTwo.HasValue
        || t.FinalTimeoutsPerSet.HasValue || t.FinalTimeoutDurationSeconds.HasValue;

    private static bool HasAnySemifinalRule(Tournament t) =>
        t.SemifinalPointsPerSet.HasValue || t.SemifinalLastSetPoints.HasValue || t.SemifinalSetsToWin.HasValue
        || t.SemifinalTotalSets.HasValue || t.SemifinalWinByTwo.HasValue
        || t.SemifinalTimeoutsPerSet.HasValue || t.SemifinalTimeoutDurationSeconds.HasValue;

    private static IReadOnlyList<TournamentStandingDto> ComputeStandings(Tournament t)
    {
        // Standings are most meaningful for round-robin but harmless to compute for any format.
        var byId = t.Participants.ToDictionary(p => p.Id);
        var stats = t.Participants.ToDictionary(p => p.Id, _ => (Wins: 0, Losses: 0, Played: 0));

        foreach (var m in t.Matches.Where(x => x.Status is TournamentMatchStatus.Completed or TournamentMatchStatus.Walkover))
        {
            if (m.WinnerParticipantId is { } w && stats.ContainsKey(w))
                stats[w] = (stats[w].Wins + 1, stats[w].Losses, stats[w].Played + 1);
            if (m.LoserParticipantId is { } l && stats.ContainsKey(l))
                stats[l] = (stats[l].Wins, stats[l].Losses + 1, stats[l].Played + 1);
        }

        return stats
            .Select(kv => new TournamentStandingDto(
                kv.Key,
                byId[kv.Key].TeamName,
                kv.Value.Played,
                kv.Value.Wins,
                kv.Value.Losses))
            .OrderByDescending(s => s.Wins)
            .ThenBy(s => s.Losses)
            .ThenBy(s => s.TeamName)
            .ToList();
    }
}
