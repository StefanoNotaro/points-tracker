using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Infrastructure.Services;

public class CounterMapper(ICounterAuthorizationService authService) : ICounterMapper
{
    public CounterDto ToDto(Counter counter, Guid? actorUserId, string? shareToken)
    {
        var access = authService.GetAccess(counter, actorUserId, null, shareToken);

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
            counter.UpdatedAt
        );
    }
}
