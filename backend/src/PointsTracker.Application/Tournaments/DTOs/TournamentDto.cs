namespace PointsTracker.Application.Tournaments.DTOs;

public record TournamentDto(
    Guid Id,
    string Name,
    string SportType,
    string Format,
    string Status,
    Guid? OwnerUserId,
    bool IsOwner,
    bool CanEdit,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? StartsAt,
    DateTime? EndsAt,
    TournamentRulesDto Rules,
    IReadOnlyList<TournamentParticipantDto> Participants,
    IReadOnlyList<TournamentMatchDto> Matches,
    IReadOnlyList<TournamentStandingDto> Standings
);

public record TournamentRulesDto(
    int? CustomPointsPerSet,
    int? CustomLastSetPoints,
    int? CustomSetsToWin,
    int? CustomTotalSets,
    bool? CustomWinByTwo,
    int? IndoorSwitchEverySets,
    bool BeachAutoSwitchSides,
    int? CustomTimeoutsPerSet,
    int? CustomTimeoutDurationSeconds,
    int? GroupCount,
    int? AdvancePerGroup,
    StageRulesDto? FinalRules,
    StageRulesDto? SemifinalRules
);

public record StageRulesDto(
    int? PointsPerSet,
    int? LastSetPoints,
    int? SetsToWin,
    int? TotalSets,
    bool? WinByTwo,
    int? TimeoutsPerSet,
    int? TimeoutDurationSeconds
);

public record TournamentParticipantDto(
    Guid Id,
    string TeamName,
    int? Seed,
    Guid? UserId
);

public record TournamentMatchDto(
    Guid Id,
    string BracketSide,
    int RoundNumber,
    int MatchNumber,
    int? GroupNumber,
    Guid? ParticipantAId,
    string? ParticipantAName,
    Guid? ParticipantBId,
    string? ParticipantBName,
    Guid? CounterId,
    Guid? WinnerParticipantId,
    string Status,
    Guid? NextMatchId,
    Guid? NextLoserMatchId,
    DateTime? ScheduledAt
);

public record TournamentSummaryDto(
    Guid Id,
    string Name,
    string SportType,
    string Format,
    string Status,
    int ParticipantCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TournamentStandingDto(
    Guid ParticipantId,
    string TeamName,
    int MatchesPlayed,
    int Wins,
    int Losses
);

public record CreateTournamentResponseDto(TournamentDto Tournament, string? SessionToken);
