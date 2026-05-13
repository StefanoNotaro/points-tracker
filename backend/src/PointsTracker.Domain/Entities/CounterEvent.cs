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

    // True when this event has been undone and its effect is currently rolled back.
    // Score events flip this true on undo and back to false on redo.
    public bool IsUndone { get; set; }

    // For undo / redo events, points to the original score event they affect.
    // Lets the UI render "Undid: Team A +1" without reconstructing the timeline.
    public Guid? RelatedEventId { get; init; }
}
