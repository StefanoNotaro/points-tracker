using PointsTracker.Domain.Enums;

namespace PointsTracker.Application.Counters.DTOs;

public record CounterDto(
    Guid Id,
    string SportType,
    // OwnerUserId is exposed so SignalR can dispatch updates to a per-user
    // group (the dashboard subscribes to all of the signed-in user's
    // counters). Anonymous counters have no owner — null is expected.
    Guid? OwnerUserId,
    string TeamAName,
    string TeamBName,
    string Status,
    IReadOnlyList<CounterSetDto> Sets,
    int CurrentSetNumber,
    int SetsWonA,
    int SetsWonB,
    int CurrentScoreA,
    int CurrentScoreB,
    bool IsOwner,
    bool CanEdit,
    bool CanScore,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SportRulesDto Rules,
    int SideSwitchCount,
    bool PendingSideSwitchConfirmation,
    int? IndoorSwitchEverySets,
    bool BeachAutoSwitchSides,
    bool CanUndo,
    bool CanRedo,
    int TimeoutsRemainingA,
    int TimeoutsRemainingB,
    ActiveTimeoutDto? ActiveTimeout,
    IReadOnlyList<CounterEventDto> Events,
    LinkedTournamentDto? LinkedTournament
);

public record LinkedTournamentDto(Guid TournamentId, string TournamentName, Guid MatchId);

public record ActiveTimeoutDto(
    string Team,
    DateTime StartedAt,
    int DurationSeconds);

public record CounterEventDto(
    Guid Id,
    int SetNumber,
    string EventType,
    string Team,
    int ScoreABefore,
    int ScoreBBefore,
    int ScoreAAfter,
    int ScoreBAfter,
    bool IsUndone,
    Guid? RelatedEventId,
    DateTime CreatedAt
);

public record SportRulesDto(
    int PointsPerSet,
    int LastSetPoints,
    int SetsToWin,
    int TotalSets,
    bool WinByTwo,
    string SideSwitchMode,
    int SideSwitchInterval,
    int SideSwitchIntervalLastSet,
    int TimeoutsPerSet,
    int TimeoutDurationSeconds);

public record CounterSummaryDto(
    Guid Id,
    string SportType,
    string TeamAName,
    string TeamBName,
    string Status,
    int SetsWonA,
    int SetsWonB,
    int CurrentScoreA,
    int CurrentScoreB,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CounterSetDto(
    int SetNumber,
    int ScoreA,
    int ScoreB,
    string? Winner
);

public record CreateCounterResponseDto(CounterDto Counter, string? SessionToken);

public record ShareTokenDto(string Token, string ShareUrl, string Scope, DateTime ExpiresAt);
