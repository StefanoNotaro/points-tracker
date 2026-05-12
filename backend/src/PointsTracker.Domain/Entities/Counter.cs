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

    private readonly List<CounterSet> _sets = [];
    public IReadOnlyList<CounterSet> Sets => _sets.AsReadOnly();

    private readonly List<CounterEvent> _events = [];
    public IReadOnlyList<CounterEvent> Events => _events.AsReadOnly();

    private readonly List<ShareToken> _shareTokens = [];
    public IReadOnlyList<ShareToken> ShareTokens => _shareTokens.AsReadOnly();

    private Counter() { }

    public static Counter Create(SportType sportType, string teamAName, string teamBName,
        Guid? ownerUserId, string? sessionTokenHash)
    {
        var counter = new Counter
        {
            SportType = sportType,
            TeamAName = teamAName,
            TeamBName = teamBName,
            OwnerUserId = ownerUserId,
            SessionTokenHash = sessionTokenHash
        };
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
        var sportRules = SportRules.For(SportType);
        CurrentSet.Increment(team);
        RecordEvent(team == Team.A ? "score_increment" : "score_increment", team, actorUserId);

        if (sportRules.IsSetWon(CurrentSet))
        {
            var winner = sportRules.SetWinner(CurrentSet)!.Value;
            CurrentSet.Complete(winner);
            var setsWon = winner == Team.A ? SetsWonA : SetsWonB;

            if (setsWon >= sportRules.SetsToWin)
            {
                Status = CounterStatus.Finished;
            }
            else
            {
                _sets.Add(CounterSet.StartNew(Id, CurrentSetNumber + 1));
            }
        }

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

        _events.Add(new CounterEvent
        {
            Id = Guid.NewGuid(),
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
