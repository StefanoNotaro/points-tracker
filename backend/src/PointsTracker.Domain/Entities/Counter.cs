using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Entities;

public class Counter
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public SportType SportType { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public string? SessionTokenHash { get; private set; }
    public string TeamAName { get; private set; } = string.Empty;
    public string TeamBName { get; private set; } = string.Empty;
    public CounterStatus Status { get; private set; } = CounterStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    // Optional rule overrides — set when SportType is Custom, or when the user
    // wants to tweak a built-in sport's points/sets configuration.
    public int? CustomPointsPerSet { get; private set; }
    public int? CustomLastSetPoints { get; private set; }
    public int? CustomSetsToWin { get; private set; }
    public int? CustomTotalSets { get; private set; }
    public bool? CustomWinByTwo { get; private set; }

    // Side-switch state
    public int SideSwitchCount { get; private set; }
    public bool PendingSideSwitchConfirmation { get; private set; }
    // For indoor volleyball "pro mode": user-selected number of sets between switches (1 or 2).
    // Null falls back to the sport's default rule.
    public int? IndoorSwitchEverySets { get; private set; }
    // Beach volleyball only: when false the server stops auto-switching at the
    // points boundary; the user is expected to use the manual switch action.
    public bool BeachAutoSwitchSides { get; private set; } = true;

    // Optional timeout-rule overrides. Null falls back to the sport's defaults
    // (see SportRules.For). Server enforces the count, the duration is shown
    // to the user as a countdown.
    public int? CustomTimeoutsPerSet { get; private set; }
    public int? CustomTimeoutDurationSeconds { get; private set; }

    // Tournament linkage — persisted so every DTO build (including SignalR
    // broadcasts) can include the "back to tournament" hint without an extra
    // DB query. Set by Tournament when a counter is spawned for a match.
    public Guid? LinkedTournamentId { get; private set; }
    public Guid? LinkedTournamentMatchId { get; private set; }
    public string? LinkedTournamentName { get; private set; }

    public SportRules EffectiveRules
    {
        get
        {
            if (CustomPointsPerSet.HasValue
                && CustomLastSetPoints.HasValue
                && CustomSetsToWin.HasValue
                && CustomTotalSets.HasValue
                && CustomWinByTwo.HasValue)
            {
                // Custom point/set/winByTwo overrides do NOT change the side-switch
                // policy — that follows the sport. Otherwise, expanding the rules
                // panel for beach volleyball (which forces customRules to be sent)
                // would silently disable the auto-switch behaviour.
                var (mode, interval, lastInterval) = SportType == SportType.Custom
                    ? (Enums.SideSwitchMode.None, 0, 0)
                    : (SportRules.For(SportType).SideSwitchMode,
                       SportRules.For(SportType).SideSwitchInterval,
                       SportRules.For(SportType).SideSwitchIntervalLastSet);

                return new SportRules(
                    CustomPointsPerSet.Value,
                    CustomLastSetPoints.Value,
                    CustomSetsToWin.Value,
                    CustomTotalSets.Value,
                    CustomWinByTwo.Value,
                    mode,
                    interval,
                    lastInterval,
                    CustomTimeoutsPerSet ?? DefaultTimeoutsForSport(SportType),
                    CustomTimeoutDurationSeconds ?? DefaultTimeoutDurationForSport(SportType));
            }
            var sport = SportRules.For(SportType);
            // Even without the full custom rules block, a timeout-only override
            // (e.g. "indoor with 3 timeouts per set") must take precedence.
            if (CustomTimeoutsPerSet.HasValue || CustomTimeoutDurationSeconds.HasValue)
            {
                return sport with
                {
                    TimeoutsPerSet = CustomTimeoutsPerSet ?? sport.TimeoutsPerSet,
                    TimeoutDurationSeconds = CustomTimeoutDurationSeconds ?? sport.TimeoutDurationSeconds,
                };
            }
            return sport;
        }
    }

    private static int DefaultTimeoutsForSport(SportType sport) =>
        sport == SportType.Custom ? 2 : SportRules.For(sport).TimeoutsPerSet;

    private static int DefaultTimeoutDurationForSport(SportType sport) =>
        sport == SportType.Custom ? 30 : SportRules.For(sport).TimeoutDurationSeconds;

    // Return the backing field directly. AsReadOnly() returned a new
    // ReadOnlyCollection<T> wrapper on every property access, which can confuse
    // EF Core's change detector when it walks collection navigations.
    private readonly List<CounterSet> _sets = [];
    public IReadOnlyList<CounterSet> Sets => _sets;

    private readonly List<CounterEvent> _events = [];
    public IReadOnlyList<CounterEvent> Events => _events;

    private readonly List<ShareToken> _shareTokens = [];
    public IReadOnlyList<ShareToken> ShareTokens => _shareTokens;

    private Counter() { }

    public static Counter Create(SportType sportType, string teamAName, string teamBName,
        Guid? ownerUserId, string? sessionTokenHash, SportRules? customRules = null,
        int? indoorSwitchEverySets = null, bool beachAutoSwitchSides = true,
        int? customTimeoutsPerSet = null, int? customTimeoutDurationSeconds = null)
    {
        if (sportType == SportType.Custom && customRules is null)
            throw new DomainException("Custom sport requires explicit rules.");
        if (indoorSwitchEverySets is not null and not (1 or 2))
            throw new DomainException("Indoor side-switch interval must be 1 or 2.");
        if (customTimeoutsPerSet is < 0 or > 9)
            throw new DomainException("Timeouts per set must be between 0 and 9.");
        if (customTimeoutDurationSeconds is < 5 or > 600)
            throw new DomainException("Timeout duration must be between 5 and 600 seconds.");

        var counter = new Counter
        {
            SportType = sportType,
            TeamAName = teamAName,
            TeamBName = teamBName,
            OwnerUserId = ownerUserId,
            SessionTokenHash = sessionTokenHash,
            IndoorSwitchEverySets = indoorSwitchEverySets,
            BeachAutoSwitchSides = beachAutoSwitchSides,
            CustomTimeoutsPerSet = customTimeoutsPerSet,
            CustomTimeoutDurationSeconds = customTimeoutDurationSeconds,
        };
        if (customRules is not null)
        {
            counter.CustomPointsPerSet = customRules.PointsPerSet;
            counter.CustomLastSetPoints = customRules.LastSetPoints;
            counter.CustomSetsToWin = customRules.SetsToWin;
            counter.CustomTotalSets = customRules.TotalSets;
            counter.CustomWinByTwo = customRules.WinByTwo;
        }
        counter._sets.Add(CounterSet.StartNew(counter.Id, 1));
        return counter;
    }

    /// <summary>
    /// Number of timeouts the given team still has available in the current
    /// set. Canceled timeouts (IsUndone) are refunded.
    /// </summary>
    public int TimeoutsRemaining(Team team)
    {
        var allowance = EffectiveRules.TimeoutsPerSet;
        var used = _events.Count(e =>
            e.EventType == "timeout"
            && !e.IsUndone
            && e.SetNumber == CurrentSetNumber
            && e.Team == team);
        return Math.Max(0, allowance - used);
    }

    public void CallTimeout(Team team, Guid? actorUserId)
    {
        EnsureActive();
        if (TimeoutsRemaining(team) <= 0)
            throw new DomainException("No timeouts remaining for this team in the current set.");

        RecordEvent("timeout", team, actorUserId);
        Touch();
    }

    /// <summary>
    /// The currently-running timeout, if any: the most recent non-canceled
    /// timeout event in the current set whose duration has not yet elapsed.
    /// </summary>
    public CounterEvent? GetActiveTimeout(DateTime now)
    {
        var duration = TimeSpan.FromSeconds(EffectiveRules.TimeoutDurationSeconds);
        var last = _events
            .Where(e => e.EventType == "timeout"
                        && !e.IsUndone
                        && e.SetNumber == CurrentSetNumber)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();
        if (last is null) return null;
        if (now >= last.CreatedAt + duration) return null;
        return last;
    }

    /// <summary>
    /// Cancel the currently-running timeout (e.g. mis-click). The original
    /// timeout event is marked as canceled — which refunds the team's
    /// allowance — and a small audit event is recorded for the history log.
    /// </summary>
    public void CancelTimeout(Guid? actorUserId)
    {
        EnsureActive();
        var active = GetActiveTimeout(DateTime.UtcNow)
            ?? throw new DomainException("No active timeout to cancel.");

        active.IsUndone = true;
        RecordEvent("timeout_canceled", active.Team, actorUserId, relatedEventId: active.Id);
        Touch();
    }

    public CounterSet CurrentSet => _sets.Last();
    public int CurrentSetNumber => CurrentSet.SetNumber;
    public int CurrentScoreA => CurrentSet.ScoreA;
    public int CurrentScoreB => CurrentSet.ScoreB;
    public int SetsWonA => _sets.Count(s => s.Winner == Team.A);
    public int SetsWonB => _sets.Count(s => s.Winner == Team.B);

    public void IncrementScore(Team team, Guid? actorUserId)
    {
        EnsureActive();
        var sportRules = EffectiveRules;
        CurrentSet.Increment(team);
        RecordEvent("score_increment", team, actorUserId);

        if (sportRules.IsSetWon(CurrentSet))
        {
            var winner = sportRules.SetWinner(CurrentSet)!.Value;
            var completedSetNumber = CurrentSet.SetNumber;
            CurrentSet.Complete(winner);
            var setsWon = winner == Team.A ? SetsWonA : SetsWonB;

            if (setsWon >= sportRules.SetsToWin)
            {
                Status = CounterStatus.Finished;
            }
            else
            {
                // Indoor: queue a confirmation prompt before the new set starts.
                if (sportRules.ShouldConfirmSwitchAfterSet(completedSetNumber, IndoorSwitchEverySets))
                    PendingSideSwitchConfirmation = true;

                _sets.Add(CounterSet.StartNew(Id, CurrentSetNumber + 1));
            }
        }
        else if (sportRules.ShouldAutoSwitchAfterPoint(CurrentSet))
        {
            // Beach: rules say a switch is due. With auto-switch enabled the server
            // performs it immediately (client shows a 5s info dialog). With auto-switch
            // disabled the server raises a pending-confirmation flag so the client can
            // ask the user whether to switch.
            if (BeachAutoSwitchSides) SideSwitchCount++;
            else PendingSideSwitchConfirmation = true;
        }

        Touch();
    }

    public void ConfirmSideSwitch()
    {
        if (!PendingSideSwitchConfirmation) return;
        SideSwitchCount++;
        PendingSideSwitchConfirmation = false;
        Touch();
    }

    public void DismissSideSwitch()
    {
        if (!PendingSideSwitchConfirmation) return;
        PendingSideSwitchConfirmation = false;
        Touch();
    }

    /// <summary>
    /// User-initiated side switch. Always allowed — the user knows the court better
    /// than the server's rule heuristic. Clears any pending confirmation as well.
    /// </summary>
    public void SwitchSidesManually()
    {
        EnsureActive();
        SideSwitchCount++;
        PendingSideSwitchConfirmation = false;
        Touch();
    }

    public void DecrementScore(Team team, Guid? actorUserId)
    {
        EnsureActive();
        CurrentSet.Decrement(team);
        RecordEvent("score_decrement", team, actorUserId);
        Touch();
    }

    /// <summary>
    /// True when there is at least one active score event that can be rolled back.
    /// </summary>
    public bool CanUndo => _events.Any(IsUndoTarget);

    /// <summary>
    /// True when the latest event is an undo/redo (so a redo is the natural next
    /// action) AND there is an undone score event waiting to be re-applied.
    /// A new score action implicitly clears the redo stack.
    /// </summary>
    public bool CanRedo
    {
        get
        {
            var last = _events.LastOrDefault();
            if (last is null) return false;
            if (last.EventType != "undo" && last.EventType != "redo") return false;
            return _events.Any(e => IsScoreEvent(e) && e.IsUndone);
        }
    }

    public void Undo(Guid? actorUserId)
    {
        EnsureActive();
        // Walk backwards through events. The first score event that is not already
        // undone is the one we roll back. Repeated calls keep stepping further back.
        var target = _events
            .AsEnumerable()
            .Reverse()
            .FirstOrDefault(IsUndoTarget);
        if (target is null) return;

        // Restore the current set's score to the state BEFORE the target event.
        // (Same simplification as before: cross-set undo is out of scope.)
        if (target.SetNumber == CurrentSetNumber)
            CurrentSet.SetScores(target.ScoreABefore, target.ScoreBBefore);

        target.IsUndone = true;
        RecordEvent("undo", target.Team, actorUserId, relatedEventId: target.Id);
        Touch();
    }

    public void Redo(Guid? actorUserId)
    {
        EnsureActive();
        if (!CanRedo) return;

        // Re-apply the most recently undone score event.
        var target = _events
            .AsEnumerable()
            .Reverse()
            .FirstOrDefault(e => IsScoreEvent(e) && e.IsUndone);
        if (target is null) return;

        if (target.SetNumber == CurrentSetNumber)
            CurrentSet.SetScores(target.ScoreAAfter, target.ScoreBAfter);

        target.IsUndone = false;
        RecordEvent("redo", target.Team, actorUserId, relatedEventId: target.Id);
        Touch();
    }

    private static bool IsScoreEvent(CounterEvent e) =>
        e.EventType == "score_increment" || e.EventType == "score_decrement";

    private static bool IsUndoTarget(CounterEvent e) =>
        IsScoreEvent(e) && !e.IsUndone;

    public void UpdateTeamName(Team team, string name)
    {
        if (team == Team.A) TeamAName = name;
        else TeamBName = name;
        Touch();
    }

    public void LinkToTournament(Guid tournamentId, Guid matchId, string tournamentName)
    {
        LinkedTournamentId      = tournamentId;
        LinkedTournamentMatchId = matchId;
        LinkedTournamentName    = tournamentName;
        Touch();
    }

    public void UpdateLinkedTournamentName(string tournamentName)
    {
        if (LinkedTournamentId is null) return;
        LinkedTournamentName = tournamentName;
        Touch();
    }

    public void ClaimByUser(Guid userId)
    {
        if (OwnerUserId.HasValue) throw new DomainException("Counter already owned.");
        OwnerUserId = userId;
        SessionTokenHash = null;
        Touch();
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Force the match to finish before the regular set-win condition is met
    /// (e.g. time-capped tournament matches). The winner is determined by
    /// sets won; ties are broken by total points across all sets (current set
    /// inclusive). On a perfect tie A wins by virtue of being first in the
    /// listing — callers should expose this convention in the UI.
    /// </summary>
    public void EndMatchManually(Guid? actorUserId)
    {
        EnsureActive();

        if (CurrentSet.Winner is null)
        {
            // Close the current set off in the same direction the score points to,
            // so set tallies and points totals stay consistent.
            var winner = CurrentScoreA == CurrentScoreB
                ? Team.A
                : (CurrentScoreA > CurrentScoreB ? Team.A : Team.B);
            CurrentSet.Complete(winner);
        }

        RecordEvent("match_ended", DetermineOverallWinner(), actorUserId);
        Status = CounterStatus.Finished;
        Touch();
    }

    private Team DetermineOverallWinner()
    {
        if (SetsWonA != SetsWonB) return SetsWonA > SetsWonB ? Team.A : Team.B;

        var pointsA = _sets.Sum(s => s.ScoreA);
        var pointsB = _sets.Sum(s => s.ScoreB);
        return pointsA >= pointsB ? Team.A : Team.B;
    }

    private void EnsureActive()
    {
        if (Status != CounterStatus.Active)
            throw new DomainException("Counter is not active.");
    }

    private void RecordEvent(string eventType, Team team, Guid? actorUserId, Guid? relatedEventId = null)
    {
        var previous = _events.LastOrDefault();
        var scoreABefore = previous?.ScoreAAfter ?? 0;
        var scoreBBefore = previous?.ScoreBAfter ?? 0;

        // NOTE: do NOT set Id here. EF Core's change-detector marks an entity added
        // to a tracked navigation as Modified when its PK is already populated
        // (it assumes you're attaching an existing row). Leaving Id at Guid.Empty
        // lets EF treat the entity as Added and generate a fresh Id on save.
        _events.Add(new CounterEvent
        {
            CounterId = Id,
            SetNumber = (short)CurrentSetNumber,
            EventType = eventType,
            Team = team,
            ScoreABefore = (short)scoreABefore,
            ScoreBBefore = (short)scoreBBefore,
            ScoreAAfter = (short)CurrentScoreA,
            ScoreBAfter = (short)CurrentScoreB,
            ActorUserId = actorUserId,
            CreatedAt = DateTime.UtcNow,
            RelatedEventId = relatedEventId
        });
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
