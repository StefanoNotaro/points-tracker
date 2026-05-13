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
                    lastInterval);
            }
            return SportRules.For(SportType);
        }
    }

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
        int? indoorSwitchEverySets = null, bool beachAutoSwitchSides = true)
    {
        if (sportType == SportType.Custom && customRules is null)
            throw new DomainException("Custom sport requires explicit rules.");
        if (indoorSwitchEverySets is not null and not (1 or 2))
            throw new DomainException("Indoor side-switch interval must be 1 or 2.");

        var counter = new Counter
        {
            SportType = sportType,
            TeamAName = teamAName,
            TeamBName = teamBName,
            OwnerUserId = ownerUserId,
            SessionTokenHash = sessionTokenHash,
            IndoorSwitchEverySets = indoorSwitchEverySets,
            BeachAutoSwitchSides = beachAutoSwitchSides
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

    public void Undo(Guid? actorUserId)
    {
        EnsureActive();
        var last = _events.LastOrDefault(e => e.EventType != "undo");
        if (last is null) return;

        CurrentSet.SetScores(last.ScoreABefore, last.ScoreBBefore);
        RecordEvent("undo", last.Team, actorUserId);
        Touch();
    }

    public void UpdateTeamName(Team team, string name)
    {
        if (team == Team.A) TeamAName = name;
        else TeamBName = name;
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

    private void EnsureActive()
    {
        if (Status != CounterStatus.Active)
            throw new DomainException("Counter is not active.");
    }

    private void RecordEvent(string eventType, Team team, Guid? actorUserId)
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
            CreatedAt = DateTime.UtcNow
        });
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
