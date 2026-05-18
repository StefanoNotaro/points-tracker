namespace PointsTracker.Domain.Entities;

/// <summary>
/// A revocable, match-scoped invite token that grants scorer-level access to a
/// specific tournament match. Anonymous-capable: the bearer just needs the raw
/// token; no user account is required. Valid until the match ends OR until
/// explicitly revoked by the organiser. See docs/ROLES_PERMISSIONS.md.
/// </summary>
public class MatchScorerLink
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TournamentId { get; private set; }
    public Guid MatchId { get; private set; }

    /// <summary>SHA-256 hash of the raw invite token. The raw token is returned
    /// once at creation time and never persisted in plaintext.</summary>
    public required string TokenHash { get; init; }

    /// <summary>Authenticated user this link was granted to. Null for anonymous links.</summary>
    public Guid? GrantedToUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }
    public string? Label { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private MatchScorerLink() { }

    public static MatchScorerLink Create(
        Guid tournamentId,
        Guid matchId,
        string tokenHash,
        Guid createdByUserId,
        Guid? grantedToUserId = null,
        string? label = null) =>
        new()
        {
            TournamentId = tournamentId,
            MatchId = matchId,
            TokenHash = tokenHash,
            CreatedByUserId = createdByUserId,
            GrantedToUserId = grantedToUserId,
            Label = label
        };

    public bool IsActive => RevokedAt is null;

    public void Revoke() => RevokedAt = DateTime.UtcNow;
}
