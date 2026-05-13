using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

public record SportRules(
    int PointsPerSet,
    int LastSetPoints,
    int SetsToWin,
    int TotalSets,
    bool WinByTwo,
    SideSwitchMode SideSwitchMode = SideSwitchMode.None,
    int SideSwitchInterval = 0,
    int SideSwitchIntervalLastSet = 0)
{
    public static SportRules For(SportType sport) => sport switch
    {
        SportType.Volleyball =>
            new(25, 15, 3, 5, true, SideSwitchMode.ConfirmEverySets, 1, 1),
        SportType.BeachVolleyball =>
            new(21, 15, 2, 3, true, SideSwitchMode.AutoEveryPoints, 7, 5),
        SportType.Beach_Volleyball =>
            new(21, 15, 2, 3, true, SideSwitchMode.AutoEveryPoints, 7, 5),
        SportType.Custom => throw new ArgumentException("Custom sport requires explicit rules; use Counter.EffectiveRules."),
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

    /// <summary>
    /// Beach-style auto switch: returns true when the set's combined point total has
    /// just crossed a multiple of the configured interval (5 in last set, 7 otherwise).
    /// </summary>
    public bool ShouldAutoSwitchAfterPoint(CounterSet set)
    {
        if (SideSwitchMode != SideSwitchMode.AutoEveryPoints) return false;
        var interval = IsLastSet(set) ? SideSwitchIntervalLastSet : SideSwitchInterval;
        if (interval <= 0) return false;
        var total = set.ScoreA + set.ScoreB;
        return total > 0 && total % interval == 0;
    }

    /// <summary>
    /// Indoor-style confirmation: returns true when the just-completed set is on a
    /// boundary that requires a side switch (every Nth completed set, default N=1).
    /// </summary>
    public bool ShouldConfirmSwitchAfterSet(int completedSetNumber, int? overrideInterval = null)
    {
        if (SideSwitchMode != SideSwitchMode.ConfirmEverySets) return false;
        var interval = overrideInterval ?? SideSwitchInterval;
        if (interval <= 0) return false;
        return completedSetNumber % interval == 0;
    }

    private bool IsLastSet(CounterSet set) => set.SetNumber == TotalSets;
}
