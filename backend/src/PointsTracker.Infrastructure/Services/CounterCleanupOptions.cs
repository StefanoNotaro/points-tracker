namespace PointsTracker.Infrastructure.Services;

/// <summary>
/// Configuration for the background cleanup of orphaned (anonymous) counters.
/// Bound from the "Cleanup" section of appsettings.
/// </summary>
public class CounterCleanupOptions
{
    public const string SectionName = "Cleanup";

    /// <summary>
    /// Master switch. Set to false to disable the worker entirely (useful for
    /// local development, tests, or CI runs).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often the worker wakes up to scan for stale counters.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Anonymous counters (no owner) that haven't been touched in this many
    /// days are soft-deleted. Authenticated users' counters are never
    /// touched here — they manage their own data.
    /// </summary>
    public int AnonymousInactiveDays { get; set; } = 14;

    /// <summary>
    /// After a counter has been soft-deleted for this many days, the row
    /// (and its child sets/events/share tokens via cascade) is permanently
    /// removed. Gives a short recovery window before bytes are gone.
    /// </summary>
    public int HardDeleteGraceDays { get; set; } = 30;

    /// <summary>
    /// Anonymous tournaments whose status is Completed/Abandoned are
    /// soft-deleted after this many days even if their UpdatedAt is recent
    /// (the bracket-result write touches UpdatedAt at completion time).
    /// </summary>
    public int TournamentCompletedRetentionDays { get; set; } = 90;
}
