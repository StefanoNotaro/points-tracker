using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

public class CounterSet
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CounterId { get; private set; }
    public int SetNumber { get; private set; }
    public int ScoreA { get; private set; }
    public int ScoreB { get; private set; }
    public Team? Winner { get; private set; }
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; private set; }

    private CounterSet() { }

    internal static CounterSet StartNew(Guid counterId, int setNumber) =>
        new() { CounterId = counterId, SetNumber = setNumber };

    internal void Increment(Team team)
    {
        if (team == Team.A) ScoreA++;
        else ScoreB++;
    }

    internal void Decrement(Team team)
    {
        if (team == Team.A) ScoreA = Math.Max(0, ScoreA - 1);
        else ScoreB = Math.Max(0, ScoreB - 1);
    }

    internal void SetScores(int scoreA, int scoreB)
    {
        ScoreA = scoreA;
        ScoreB = scoreB;
    }

    internal void Complete(Team winner)
    {
        Winner = winner;
        EndedAt = DateTime.UtcNow;
    }
}
