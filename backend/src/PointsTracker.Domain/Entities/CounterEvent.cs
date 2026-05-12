using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

public class CounterEvent
{
    public Guid Id { get; init; }
    public Guid CounterId { get; init; }
    public short SetNumber { get; init; }
    public required string EventType { get; init; }
    public Team Team { get; init; }
    public short ScoreABefore { get; init; }
    public short ScoreBBefore { get; init; }
    public short ScoreAAfter { get; init; }
    public short ScoreBAfter { get; init; }
    public Guid? ActorUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
