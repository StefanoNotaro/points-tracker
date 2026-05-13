using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Infrastructure.Services;

public class CounterMapper(ICounterAuthorizationService authService) : ICounterMapper
{
    public CounterDto ToDto(Counter counter, Guid? actorUserId, string? sessionToken, string? shareToken)
    {
        var access = authService.GetAccess(counter, actorUserId, sessionToken, shareToken);

        var rules = counter.EffectiveRules;
        return new CounterDto(
            counter.Id,
            counter.SportType.ToString().ToLowerInvariant(),
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
                rules.SideSwitchIntervalLastSet),
            counter.SideSwitchCount,
            counter.PendingSideSwitchConfirmation,
            counter.IndoorSwitchEverySets,
            counter.BeachAutoSwitchSides,
            counter.CanUndo,
            counter.CanRedo,
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
                .ToList()
        );
    }
}
