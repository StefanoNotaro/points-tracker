using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Infrastructure.Services;

public class CounterMapper(ICounterAuthorizationService authService) : ICounterMapper
{
    public CounterDto ToDto(Counter counter, Guid? actorUserId, string? sessionToken, string? shareToken, bool canScore = false)
    {
        var access = authService.GetAccess(counter, actorUserId, sessionToken, shareToken);

        var rules = counter.EffectiveRules;
        return new CounterDto(
            counter.Id,
            counter.SportType.ToString().ToLowerInvariant(),
            counter.OwnerUserId,
            counter.TeamAName,
            counter.TeamBName,
            counter.Status.ToString().ToLowerInvariant(),
            counter.Sets.Select(s => new CounterSetDto(
                s.SetNumber, s.ScoreA, s.ScoreB, s.Winner?.ToString())).ToList(),
            counter.CurrentSetNumber,
            counter.SetsWonA,
            counter.SetsWonB,
            counter.CurrentScoreA,
            counter.CurrentScoreB,
            access.IsOwner,
            access.CanEdit,
            canScore,
            counter.CreatedAt,
            counter.UpdatedAt,
            new SportRulesDto(
                rules.PointsPerSet,
                rules.LastSetPoints,
                rules.SetsToWin,
                rules.TotalSets,
                rules.WinByTwo,
                rules.SideSwitchMode.ToString().ToLowerInvariant(),
                rules.SideSwitchInterval,
                rules.SideSwitchIntervalLastSet,
                rules.TimeoutsPerSet,
                rules.TimeoutDurationSeconds),
            counter.SideSwitchCount,
            counter.PendingSideSwitchConfirmation,
            counter.IndoorSwitchEverySets,
            counter.BeachAutoSwitchSides,
            counter.CanUndo,
            counter.CanRedo,
            counter.TimeoutsRemaining(Domain.Enums.Team.A),
            counter.TimeoutsRemaining(Domain.Enums.Team.B),
            counter.GetActiveTimeout(DateTime.UtcNow) is { } active
                ? new ActiveTimeoutDto(
                    active.Team.ToString(),
                    active.CreatedAt,
                    rules.TimeoutDurationSeconds)
                : null,
            counter.Events
                .OrderBy(e => e.CreatedAt)
                .Select(e => new CounterEventDto(
                    e.Id,
                    e.SetNumber,
                    e.EventType,
                    e.Team.ToString(),
                    e.ScoreABefore,
                    e.ScoreBBefore,
                    e.ScoreAAfter,
                    e.ScoreBAfter,
                    e.IsUndone,
                    e.RelatedEventId,
                    e.CreatedAt))
                .ToList(),
            LinkedTournament: counter.LinkedTournamentId is { } tid
                && counter.LinkedTournamentMatchId is { } mid
                ? new LinkedTournamentDto(tid, counter.LinkedTournamentName ?? string.Empty, mid)
                : null
        );
    }
}
