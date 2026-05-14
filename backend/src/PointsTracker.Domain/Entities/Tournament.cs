using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Entities;

public class Tournament
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public SportType SportType { get; private set; }
    public TournamentFormat Format { get; private set; }
    public TournamentStatus Status { get; private set; } = TournamentStatus.Draft;

    public Guid? OwnerUserId { get; private set; }
    public string? SessionTokenHash { get; private set; }

    // Match-rule overrides — same shape as Counter so we can reuse SportRules.For()
    // and Counter.Create(...). Null = use sport default.
    public int? CustomPointsPerSet { get; private set; }
    public int? CustomLastSetPoints { get; private set; }
    public int? CustomSetsToWin { get; private set; }
    public int? CustomTotalSets { get; private set; }
    public bool? CustomWinByTwo { get; private set; }
    public int? IndoorSwitchEverySets { get; private set; }
    public bool BeachAutoSwitchSides { get; private set; } = true;
    public int? CustomTimeoutsPerSet { get; private set; }
    public int? CustomTimeoutDurationSeconds { get; private set; }

    // Only used when Format == GroupStageElimination.
    public int? GroupCount { get; private set; }
    public int? AdvancePerGroup { get; private set; }

    // Per-stage rule overrides — applied at counter-spawn time for matches
    // identified as the final / semifinal in the Main bracket. All nullable;
    // null = use the tournament's general rules.
    public int? FinalPointsPerSet { get; private set; }
    public int? FinalLastSetPoints { get; private set; }
    public int? FinalSetsToWin { get; private set; }
    public int? FinalTotalSets { get; private set; }
    public bool? FinalWinByTwo { get; private set; }
    public int? FinalTimeoutsPerSet { get; private set; }
    public int? FinalTimeoutDurationSeconds { get; private set; }

    public int? SemifinalPointsPerSet { get; private set; }
    public int? SemifinalLastSetPoints { get; private set; }
    public int? SemifinalSetsToWin { get; private set; }
    public int? SemifinalTotalSets { get; private set; }
    public bool? SemifinalWinByTwo { get; private set; }
    public int? SemifinalTimeoutsPerSet { get; private set; }
    public int? SemifinalTimeoutDurationSeconds { get; private set; }

    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    private readonly List<TournamentParticipant> _participants = [];
    public IReadOnlyList<TournamentParticipant> Participants => _participants;

    private readonly List<TournamentMatch> _matches = [];
    public IReadOnlyList<TournamentMatch> Matches => _matches;

    private Tournament() { }

    public static Tournament Create(
        string name,
        SportType sportType,
        TournamentFormat format,
        Guid? ownerUserId,
        string? sessionTokenHash,
        SportRules? customRules = null,
        int? indoorSwitchEverySets = null,
        bool beachAutoSwitchSides = true,
        int? customTimeoutsPerSet = null,
        int? customTimeoutDurationSeconds = null,
        int? groupCount = null,
        int? advancePerGroup = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tournament name is required.");
        if (sportType == SportType.Custom && customRules is null)
            throw new DomainException("Custom sport requires explicit rules.");

        var t = new Tournament
        {
            Name = name.Trim(),
            SportType = sportType,
            Format = format,
            OwnerUserId = ownerUserId,
            SessionTokenHash = sessionTokenHash,
            IndoorSwitchEverySets = indoorSwitchEverySets,
            BeachAutoSwitchSides = beachAutoSwitchSides,
            CustomTimeoutsPerSet = customTimeoutsPerSet,
            CustomTimeoutDurationSeconds = customTimeoutDurationSeconds,
            GroupCount = format == TournamentFormat.GroupStageElimination ? (groupCount ?? 2) : null,
            AdvancePerGroup = format == TournamentFormat.GroupStageElimination ? (advancePerGroup ?? 2) : null,
        };
        if (customRules is not null) t.ApplyCustomRules(customRules);
        return t;
    }

    private void ApplyCustomRules(SportRules r)
    {
        CustomPointsPerSet = r.PointsPerSet;
        CustomLastSetPoints = r.LastSetPoints;
        CustomSetsToWin = r.SetsToWin;
        CustomTotalSets = r.TotalSets;
        CustomWinByTwo = r.WinByTwo;
    }

    /// <summary>
    /// Snapshot of the rules currently in effect — used when a counter is
    /// spawned for one of this tournament's matches.
    /// </summary>
    public SportRules? CurrentCustomRules =>
        CustomPointsPerSet.HasValue && CustomLastSetPoints.HasValue && CustomSetsToWin.HasValue
        && CustomTotalSets.HasValue && CustomWinByTwo.HasValue
            ? new SportRules(
                CustomPointsPerSet.Value,
                CustomLastSetPoints.Value,
                CustomSetsToWin.Value,
                CustomTotalSets.Value,
                CustomWinByTwo.Value)
            : null;

    /// <summary>
    /// Resolve the rule overrides that should be applied when a counter is
    /// spawned for a given match. Per-stage overrides (final / semifinal) win
    /// if set, otherwise the tournament-level CurrentCustomRules apply.
    /// </summary>
    public (SportRules? rules, int? timeoutsPerSet, int? timeoutDurationSeconds) ResolveMatchRules(TournamentMatch match)
    {
        var stage = StageOf(match);
        return stage switch
        {
            MatchStage.Final when AnyFinalRule()
                => (BuildFinalRules(), FinalTimeoutsPerSet, FinalTimeoutDurationSeconds),
            MatchStage.Semifinal when AnySemifinalRule()
                => (BuildSemifinalRules(), SemifinalTimeoutsPerSet, SemifinalTimeoutDurationSeconds),
            _ => (CurrentCustomRules, CustomTimeoutsPerSet, CustomTimeoutDurationSeconds),
        };
    }

    private bool AnyFinalRule() =>
        FinalPointsPerSet.HasValue || FinalLastSetPoints.HasValue || FinalSetsToWin.HasValue
        || FinalTotalSets.HasValue || FinalWinByTwo.HasValue;
    private bool AnySemifinalRule() =>
        SemifinalPointsPerSet.HasValue || SemifinalLastSetPoints.HasValue || SemifinalSetsToWin.HasValue
        || SemifinalTotalSets.HasValue || SemifinalWinByTwo.HasValue;

    private SportRules? BuildFinalRules()
    {
        var fallback = CurrentCustomRules ?? SportRules.For(SportType);
        return new SportRules(
            FinalPointsPerSet ?? fallback.PointsPerSet,
            FinalLastSetPoints ?? fallback.LastSetPoints,
            FinalSetsToWin ?? fallback.SetsToWin,
            FinalTotalSets ?? fallback.TotalSets,
            FinalWinByTwo ?? fallback.WinByTwo);
    }
    private SportRules? BuildSemifinalRules()
    {
        var fallback = CurrentCustomRules ?? SportRules.For(SportType);
        return new SportRules(
            SemifinalPointsPerSet ?? fallback.PointsPerSet,
            SemifinalLastSetPoints ?? fallback.LastSetPoints,
            SemifinalSetsToWin ?? fallback.SetsToWin,
            SemifinalTotalSets ?? fallback.TotalSets,
            SemifinalWinByTwo ?? fallback.WinByTwo);
    }

    public enum MatchStage { Regular, Semifinal, Final }

    private MatchStage StageOf(TournamentMatch match)
    {
        if (match.BracketSide == BracketSide.GrandFinal) return MatchStage.Final;
        if (match.BracketSide != BracketSide.Main) return MatchStage.Regular;

        var mainRounds = _matches
            .Where(m => m.BracketSide == BracketSide.Main)
            .Select(m => m.RoundNumber)
            .DefaultIfEmpty(0)
            .Max();
        if (mainRounds <= 0) return MatchStage.Regular;
        if (match.RoundNumber == mainRounds) return MatchStage.Final;
        if (match.RoundNumber == mainRounds - 1) return MatchStage.Semifinal;
        return MatchStage.Regular;
    }

    public TournamentParticipant AddParticipant(string teamName, int? seed, Guid? userId)
    {
        EnsureMutable("participants");
        if (Status == TournamentStatus.Active || Status == TournamentStatus.Completed)
            throw new DomainException("Cannot add participants once the bracket has started.");
        if (string.IsNullOrWhiteSpace(teamName))
            throw new DomainException("Participant name is required.");
        if (_participants.Any(p => string.Equals(p.TeamName, teamName.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A participant with that name already exists.");

        var p = TournamentParticipant.Create(Id, teamName.Trim(), seed, userId);
        _participants.Add(p);
        Touch();
        return p;
    }

    public void RemoveParticipant(Guid participantId)
    {
        if (Status == TournamentStatus.Active || Status == TournamentStatus.Completed)
            throw new DomainException("Cannot remove participants once the bracket has started.");
        var p = _participants.FirstOrDefault(x => x.Id == participantId)
            ?? throw new NotFoundException("Participant", participantId);
        _participants.Remove(p);
        Touch();
    }

    /// <summary>
    /// Replace the current set of matches with a freshly-generated bracket
    /// and flip status to Active. Only valid while in Draft or Registration.
    /// </summary>
    /// <summary>
    /// Minimum participant count for this tournament to be startable.
    /// Knock-outs need at least two teams; round-robin / double-elim are
    /// pointless below three / four; group stage needs each group to have
    /// at least two teams.
    /// </summary>
    public int MinTeamsToStart() => Format switch
    {
        TournamentFormat.SingleElimination     => 2,
        TournamentFormat.DoubleElimination     => 4,
        TournamentFormat.RoundRobin            => 3,
        TournamentFormat.GroupStageElimination => Math.Max(4, (GroupCount ?? 2) * 2),
        _ => 2,
    };

    public void StartWithBracket(IEnumerable<TournamentMatch> generated)
    {
        if (Status is TournamentStatus.Active or TournamentStatus.Completed)
            throw new DomainException("Tournament is already started.");
        var min = MinTeamsToStart();
        if (_participants.Count < min)
            throw new DomainException(
                $"{Format} requires at least {min} participants to start (currently {_participants.Count}).");

        _matches.Clear();
        _matches.AddRange(generated);
        Status = TournamentStatus.Active;
        StartsAt ??= DateTime.UtcNow;
        Touch();
    }

    public TournamentMatch GetMatch(Guid matchId) =>
        _matches.FirstOrDefault(m => m.Id == matchId)
        ?? throw new NotFoundException("Match", matchId);

    public void AttachCounter(Guid matchId, Guid counterId)
    {
        var match = GetMatch(matchId);
        match.AttachCounter(counterId);
        Touch();
    }

    /// <summary>
    /// Called when a match's counter signals completion. Advances winner (and
    /// loser, for double-elim) to the appropriate next match slot.
    /// </summary>
    public void RecordMatchResult(Guid matchId, Guid winnerParticipantId)
    {
        var match = GetMatch(matchId);
        match.RecordWinner(winnerParticipantId);

        // Group-stage matches don't have NextMatchId links — instead we wait
        // for the whole group to finish, then promote the top finishers into
        // the bracket's round-1 slots.
        if (match.BracketSide == BracketSide.GroupStage)
            PromoteFromCompletedGroup(match.GroupNumber!.Value);
        else
            AdvanceFrom(match);

        if (_matches.All(m => m.Status is TournamentMatchStatus.Completed
                                          or TournamentMatchStatus.Walkover))
        {
            Status = TournamentStatus.Completed;
            EndsAt = DateTime.UtcNow;
        }
        Touch();
    }

    private void PromoteFromCompletedGroup(int groupNumber)
    {
        var groupMatches = _matches
            .Where(m => m.BracketSide == BracketSide.GroupStage && m.GroupNumber == groupNumber)
            .ToList();
        if (groupMatches.Any(m => m.Status is not (TournamentMatchStatus.Completed or TournamentMatchStatus.Walkover)))
            return; // still playing

        // Rank participants in the group by wins (then total point differential).
        var participants = groupMatches
            .SelectMany(m => new[] { m.ParticipantAId, m.ParticipantBId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var ranking = participants
            .Select(pid => new
            {
                Id = pid,
                Wins = groupMatches.Count(m => m.WinnerParticipantId == pid),
                Losses = groupMatches.Count(m => m.LoserParticipantId == pid),
            })
            .OrderByDescending(x => x.Wins)
            .ThenBy(x => x.Losses)
            .Select(x => x.Id)
            .ToList();

        var advance = AdvancePerGroup ?? 0;
        if (advance == 0) return;

        // Place each advancer into the corresponding round-1 bracket slot.
        // Slot index is determined by (groupNumber - 1) * advance + rank.
        var bracketRound1 = _matches
            .Where(m => m.BracketSide == BracketSide.Main && m.RoundNumber == 1)
            .OrderBy(m => m.MatchNumber)
            .ToList();
        if (bracketRound1.Count == 0) return;

        for (var rank = 0; rank < Math.Min(advance, ranking.Count); rank++)
        {
            var globalIdx = (groupNumber - 1) * advance + rank;
            // Pair groups so the top of group N meets the bottom of the
            // opposing group (standard "1 vs 2nd" seeding across groups).
            var slotIdx = globalIdx / 2;
            var toA = globalIdx % 2 == 0;
            if (slotIdx >= bracketRound1.Count) break;

            var slot = bracketRound1[slotIdx];
            var a = toA ? (Guid?)ranking[rank] : slot.ParticipantAId;
            var b = toA ? slot.ParticipantBId : (Guid?)ranking[rank];
            slot.SetParticipants(a, b);
        }
    }

    private void AdvanceFrom(TournamentMatch match)
    {
        // Winner advances. FirstOrDefault is intentional: tolerate legacy
        // brackets that were generated before TournamentMatch.Id was
        // initialised eagerly (NextMatchId could be Guid.Empty there).
        if (match.NextMatchId is { } winnerNextId && winnerNextId != Guid.Empty)
        {
            var next = _matches.FirstOrDefault(m => m.Id == winnerNextId);
            if (next is not null)
                ApplyToSlot(next, match.WinnerParticipantId, match.WinnerToSideA);
        }

        // Loser drops (double-elim)
        if (match.NextLoserMatchId is { } loserNextId && loserNextId != Guid.Empty
            && match.LoserParticipantId is not null)
        {
            var next = _matches.FirstOrDefault(m => m.Id == loserNextId);
            if (next is not null)
                ApplyToSlot(next, match.LoserParticipantId, match.LoserToSideA);
        }
    }

    private static void ApplyToSlot(TournamentMatch next, Guid? participantId, bool toSideA)
    {
        var a = toSideA ? participantId : next.ParticipantAId;
        var b = toSideA ? next.ParticipantBId : participantId;
        next.SetParticipants(a, b);
    }

    /// <summary>
    /// Update match-rule overrides. Allowed while the tournament is running,
    /// but only future matches (Pending/Ready, no counter yet) actually pick
    /// up the new values; in-progress and completed matches keep their rules.
    /// </summary>
    public void UpdateRules(
        SportRules? customRules,
        int? indoorSwitchEverySets,
        bool beachAutoSwitchSides,
        int? customTimeoutsPerSet,
        int? customTimeoutDurationSeconds)
    {
        if (Status is TournamentStatus.Completed or TournamentStatus.Abandoned)
            throw new DomainException("Tournament is closed; rules can no longer be edited.");

        if (customRules is not null)
            ApplyCustomRules(customRules);
        else
        {
            CustomPointsPerSet = null;
            CustomLastSetPoints = null;
            CustomSetsToWin = null;
            CustomTotalSets = null;
            CustomWinByTwo = null;
        }

        IndoorSwitchEverySets = indoorSwitchEverySets;
        BeachAutoSwitchSides = beachAutoSwitchSides;
        CustomTimeoutsPerSet = customTimeoutsPerSet;
        CustomTimeoutDurationSeconds = customTimeoutDurationSeconds;

        // Touch only the future matches — Counter spawn time reads from this entity.
        foreach (var m in _matches.Where(m => m.IsFutureMatch))
            m.ApplyRuleUpdate();
        Touch();
    }

    public void UpdateStageRules(
        SportRules? finalRules, int? finalTimeoutsPerSet, int? finalTimeoutDurationSeconds,
        SportRules? semifinalRules, int? semifinalTimeoutsPerSet, int? semifinalTimeoutDurationSeconds)
    {
        if (Status is TournamentStatus.Completed or TournamentStatus.Abandoned)
            throw new DomainException("Tournament is closed.");

        FinalPointsPerSet  = finalRules?.PointsPerSet;
        FinalLastSetPoints = finalRules?.LastSetPoints;
        FinalSetsToWin     = finalRules?.SetsToWin;
        FinalTotalSets     = finalRules?.TotalSets;
        FinalWinByTwo      = finalRules?.WinByTwo;
        FinalTimeoutsPerSet         = finalTimeoutsPerSet;
        FinalTimeoutDurationSeconds = finalTimeoutDurationSeconds;

        SemifinalPointsPerSet  = semifinalRules?.PointsPerSet;
        SemifinalLastSetPoints = semifinalRules?.LastSetPoints;
        SemifinalSetsToWin     = semifinalRules?.SetsToWin;
        SemifinalTotalSets     = semifinalRules?.TotalSets;
        SemifinalWinByTwo      = semifinalRules?.WinByTwo;
        SemifinalTimeoutsPerSet         = semifinalTimeoutsPerSet;
        SemifinalTimeoutDurationSeconds = semifinalTimeoutDurationSeconds;

        Touch();
    }

    public void Rename(string newName)
    {
        EnsureMutable("metadata");
        if (string.IsNullOrWhiteSpace(newName)) throw new DomainException("Name is required.");
        Name = newName.Trim();
        Touch();
    }

    public void Schedule(DateTime? startsAt, DateTime? endsAt)
    {
        EnsureMutable("metadata");
        if (startsAt.HasValue && endsAt.HasValue && endsAt < startsAt)
            throw new DomainException("End date cannot be before start date.");
        StartsAt = startsAt;
        EndsAt = endsAt;
        Touch();
    }

    public void ClaimByUser(Guid userId)
    {
        if (OwnerUserId.HasValue) throw new DomainException("Tournament already owned.");
        OwnerUserId = userId;
        SessionTokenHash = null;
        Touch();
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    private void EnsureMutable(string what)
    {
        if (Status == TournamentStatus.Completed || Status == TournamentStatus.Abandoned)
            throw new DomainException($"Tournament is closed; {what} cannot be modified.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
