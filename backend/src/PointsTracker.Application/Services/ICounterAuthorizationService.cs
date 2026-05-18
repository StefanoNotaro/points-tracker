using PointsTracker.Domain.Entities;

namespace PointsTracker.Application.Services;

public interface ICounterAuthorizationService
{
    CounterAccess GetAccess(Counter counter, Guid? userId, string? sessionToken, string? shareToken);
    CounterAccess GetLiveAccess(Counter counter, Guid? userId, string? sessionToken, string? shareToken, bool isSuperAdmin = false);

    /// <summary>
    /// Returns true when <paramref name="rawScorerToken"/> is a valid, un-revoked
    /// scorer link for the match this counter is linked to. Returns false for
    /// counters not linked to any match, or when the token is absent/invalid.
    /// </summary>
    Task<bool> HasScorerAccessAsync(Counter counter, string? rawScorerToken, CancellationToken ct = default);
}

public record CounterAccess(bool IsOwner, bool CanEdit, bool CanRead, bool CanScore = false);
