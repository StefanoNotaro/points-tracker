using PointsTracker.Domain.Enums;

namespace PointsTracker.Application.Counters.DTOs;

public record CounterDto(
    Guid Id,
    string SportType,
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
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SportRulesDto Rules,
    int SideSwitchCount,
    bool PendingSideSwitchConfirmation,
    int? IndoorSwitchEverySets,
    bool BeachAutoSwitchSides
);

public record SportRulesDto(
    int PointsPerSet,
    int LastSetPoints,
    int SetsToWin,
    int TotalSets,
    bool WinByTwo,
    string SideSwitchMode,
    int SideSwitchInterval,
    int SideSwitchIntervalLastSet);

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
