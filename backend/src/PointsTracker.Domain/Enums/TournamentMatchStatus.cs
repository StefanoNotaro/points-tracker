namespace PointsTracker.Domain.Enums;

public enum TournamentMatchStatus
{
    // Slot exists but at least one participant is not yet known (waiting on a feeder match).
    Pending,
    // Both participants resolved; counter not yet started.
    Ready,
    // A counter is linked and the match is being played.
    InProgress,
    // Match has a winner recorded.
    Completed,
    // Either side withdrew (e.g. bye, walkover).
    Walkover
}
