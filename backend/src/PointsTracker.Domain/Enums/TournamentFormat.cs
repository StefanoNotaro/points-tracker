namespace PointsTracker.Domain.Enums;

public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    /// <summary>
    /// Round-robin within fixed-size groups, then top N from each group fill
    /// a single-elimination bracket. Group advancement is computed when all
    /// of a group's matches are Completed.
    /// </summary>
    GroupStageElimination
}
