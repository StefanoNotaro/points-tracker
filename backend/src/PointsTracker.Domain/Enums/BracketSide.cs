namespace PointsTracker.Domain.Enums;

/// <summary>
/// For single-elim + round-robin only the Main bracket is used.
/// Double-elim splits into Winners / Losers / GrandFinal.
/// </summary>
public enum BracketSide
{
    Main,
    Winners,
    Losers,
    GrandFinal,
    /// <summary>
    /// Round-robin matches that feed a knockout bracket. GroupNumber on the
    /// match identifies which group the match belongs to.
    /// </summary>
    GroupStage
}
