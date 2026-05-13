using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Entities;

/// <summary>
/// A single match slot inside a tournament. Generated up-front by the
/// IBracketGenerator strategy. ParticipantA/B may be null while the slot
/// is awaiting a feeder result. CounterId is filled lazily the first time
/// a scorer opens the match.
/// </summary>
public class TournamentMatch
{
    // Initialised eagerly so the bracket generator can wire NextMatchId /
    // NextLoserMatchId between sibling slots BEFORE SaveChanges. If Id stayed
    // at Guid.Empty until EF assigned it, every advancement pointer would
    // capture Guid.Empty and crash AdvanceFrom after the first match result.
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TournamentId { get; private set; }

    public BracketSide BracketSide { get; private set; } = BracketSide.Main;
    public int RoundNumber { get; private set; }
    public int MatchNumber { get; private set; }
    /// <summary>
    /// Only used by GroupStage matches. Null for knockout matches.
    /// </summary>
    public int? GroupNumber { get; private set; }

    public Guid? ParticipantAId { get; private set; }
    public Guid? ParticipantBId { get; private set; }

    public Guid? CounterId { get; private set; }
    public Guid? WinnerParticipantId { get; private set; }
    public Guid? LoserParticipantId { get; private set; }

    public TournamentMatchStatus Status { get; private set; } = TournamentMatchStatus.Pending;

    /// <summary>
    /// Where the winner of this match goes next. Null = final match.
    /// </summary>
    public Guid? NextMatchId { get; private set; }
    /// <summary>
    /// Double-elim only: where the loser of this match drops to.
    /// </summary>
    public Guid? NextLoserMatchId { get; private set; }
    /// <summary>
    /// When a feeder slot is filled, do we go into A or B side of the next match?
    /// True = A slot, false = B slot.
    /// </summary>
    public bool WinnerToSideA { get; private set; } = true;
    public bool LoserToSideA { get; private set; } = true;

    public DateTime? ScheduledAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private TournamentMatch() { }

    internal static TournamentMatch Create(Guid tournamentId, BracketSide side, int round, int matchNumber, int? groupNumber = null) =>
        new()
        {
            TournamentId = tournamentId,
            BracketSide  = side,
            RoundNumber  = round,
            MatchNumber  = matchNumber,
            GroupNumber  = groupNumber,
        };

    internal void SetParticipants(Guid? a, Guid? b)
    {
        ParticipantAId = a;
        ParticipantBId = b;
        if (a is not null && b is not null) Status = TournamentMatchStatus.Ready;
        Touch();
    }

    internal void LinkAdvancement(Guid? nextMatchId, bool winnerToSideA, Guid? nextLoserMatchId = null, bool loserToSideA = true)
    {
        NextMatchId      = nextMatchId;
        WinnerToSideA    = winnerToSideA;
        NextLoserMatchId = nextLoserMatchId;
        LoserToSideA     = loserToSideA;
        Touch();
    }

    internal void AttachCounter(Guid counterId)
    {
        if (Status == TournamentMatchStatus.Completed)
            throw new DomainException("Cannot attach a counter to a completed match.");
        CounterId = counterId;
        if (ParticipantAId is not null && ParticipantBId is not null)
            Status = TournamentMatchStatus.InProgress;
        Touch();
    }

    internal void RecordWinner(Guid winnerParticipantId)
    {
        if (winnerParticipantId != ParticipantAId && winnerParticipantId != ParticipantBId)
            throw new DomainException("Winner must be one of the match participants.");

        WinnerParticipantId = winnerParticipantId;
        LoserParticipantId  = winnerParticipantId == ParticipantAId ? ParticipantBId : ParticipantAId;
        Status = TournamentMatchStatus.Completed;
        Touch();
    }

    internal void GrantWalkover(Guid surviving)
    {
        WinnerParticipantId = surviving;
        LoserParticipantId  = null;
        Status = TournamentMatchStatus.Walkover;
        Touch();
    }

    internal void Schedule(DateTime? at)
    {
        ScheduledAt = at;
        Touch();
    }

    internal void ApplyRuleUpdate()
    {
        // Hook for future per-match rule resolution. The tournament-level rule
        // change is allowed only when this match has not yet started.
        if (Status is TournamentMatchStatus.InProgress or TournamentMatchStatus.Completed)
            throw new DomainException("Match is no longer mutable.");
        Touch();
    }

    public bool IsFutureMatch =>
        Status is TournamentMatchStatus.Pending or TournamentMatchStatus.Ready
        && CounterId is null;

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
