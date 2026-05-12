using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

public record SportRules(int PointsPerSet, int LastSetPoints, int SetsToWin, int TotalSets, bool WinByTwo)
{
    public static SportRules For(SportType sport) => sport switch
    {
        SportType.Volleyball => new(25, 15, 3, 5, true),
        SportType.BeachVolleyball => new(21, 15, 2, 3, true),
        _ => throw new ArgumentOutOfRangeException(nameof(sport))
    };

    public bool IsSetWon(CounterSet set)
    {
        var target = IsLastSet(set) ? LastSetPoints : PointsPerSet;
        var lead = Math.Abs(set.ScoreA - set.ScoreB);
        var max = Math.Max(set.ScoreA, set.ScoreB);
        return max >= target && (!WinByTwo || lead >= 2);
    }

    public Team? SetWinner(CounterSet set) =>
        IsSetWon(set) ? (set.ScoreA > set.ScoreB ? Team.A : Team.B) : null;

    private bool IsLastSet(CounterSet set) => set.SetNumber == TotalSets;
}
