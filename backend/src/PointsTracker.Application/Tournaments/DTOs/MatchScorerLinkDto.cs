namespace PointsTracker.Application.Tournaments.DTOs;

/// <summary>Safe to expose in list responses — does not include the raw token.</summary>
public record MatchScorerLinkDto(
    Guid Id,
    Guid TournamentId,
    Guid MatchId,
    string? Label,
    Guid? GrantedToUserId,
    bool IsActive,
    DateTime CreatedAt
);

/// <summary>One-time response returned immediately after issuing a link. Includes
/// the raw token, which is never stored and cannot be retrieved again.</summary>
public record IssuedMatchScorerLinkDto(
    Guid Id,
    Guid TournamentId,
    Guid MatchId,
    string? Label,
    Guid? GrantedToUserId,
    string Token,
    DateTime CreatedAt
);
