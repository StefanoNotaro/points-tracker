using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Brackets;

public static class BracketGeneratorFactory
{
    public static IBracketGenerator For(TournamentFormat format, int groupCount = 2, int advancePerGroup = 2)
        => format switch
        {
            TournamentFormat.SingleElimination     => new SingleEliminationGenerator(),
            TournamentFormat.RoundRobin            => new RoundRobinGenerator(),
            TournamentFormat.DoubleElimination     => new DoubleEliminationGenerator(),
            TournamentFormat.GroupStageElimination => new GroupStageEliminationGenerator(groupCount, advancePerGroup),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown tournament format.")
        };
}
