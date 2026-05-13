using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Brackets;

/// <summary>
/// Group stage (round-robin inside each group) followed by a single-elimination
/// knockout filled by the top N from each group. Bracket slots start empty
/// (Pending) and get populated when their feeder group is fully Completed —
/// see <see cref="Tournament.PromoteFromCompletedGroups"/>.
///
/// Knobs:
///   - GroupCount: how many groups to split participants into.
///   - AdvancePerGroup: top-N from each group that move into the bracket.
///
/// Constraints: bracket size = GroupCount * AdvancePerGroup must be a power
/// of two (so the knockout single-elim shape is balanced). Validated below.
/// </summary>
public sealed class GroupStageEliminationGenerator(int groupCount, int advancePerGroup) : IBracketGenerator
{
    public IReadOnlyList<TournamentMatch> Generate(
        Guid tournamentId,
        IReadOnlyList<TournamentParticipant> participants)
    {
        if (groupCount < 2) throw new DomainException("Group stage needs at least 2 groups.");
        if (advancePerGroup < 1) throw new DomainException("At least 1 team must advance per group.");
        if (participants.Count < groupCount * 2)
            throw new DomainException("Need at least 2 participants per group.");

        var bracketSize = groupCount * advancePerGroup;
        if (!IsPowerOfTwo(bracketSize))
            throw new DomainException($"Bracket size {bracketSize} (groups × advancers) must be a power of two.");

        var matches = new List<TournamentMatch>();

        // 1. Group assignment — snake-seed style for fairness if seeds are set,
        //    otherwise registration order. Group i gets every (groupCount)-th team.
        var ordered = participants
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ThenBy(p => p.RegisteredAt)
            .ToList();

        var groups = new List<List<TournamentParticipant>>();
        for (var i = 0; i < groupCount; i++) groups.Add(new List<TournamentParticipant>());
        for (var i = 0; i < ordered.Count; i++)
            groups[i % groupCount].Add(ordered[i]);

        // 2. Round-robin within each group via the standard circle method.
        for (var g = 0; g < groupCount; g++)
        {
            var groupMatches = GenerateGroupRoundRobin(tournamentId, groups[g], groupNumber: g + 1);
            matches.AddRange(groupMatches);
        }

        // 3. Single-elimination bracket — slots are created empty and filled
        //    by Tournament.PromoteFromCompletedGroups when groups finish.
        var rounds = (int)Math.Log2(bracketSize);
        var previousRoundIds = new Guid?[bracketSize / 2];
        for (var i = 0; i < bracketSize / 2; i++)
        {
            var m = TournamentMatch.Create(tournamentId, BracketSide.Main, round: 1, matchNumber: i + 1);
            matches.Add(m);
            previousRoundIds[i] = m.Id;
        }
        for (var r = 2; r <= rounds; r++)
        {
            var slots = previousRoundIds.Length / 2;
            var thisRound = new Guid?[slots];
            for (var i = 0; i < slots; i++)
            {
                var m = TournamentMatch.Create(tournamentId, BracketSide.Main, round: r, matchNumber: i + 1);
                matches.Add(m);
                thisRound[i] = m.Id;
            }
            for (var i = 0; i < previousRoundIds.Length; i++)
            {
                var feeder = matches.First(x => x.Id == previousRoundIds[i]!.Value);
                var nextSlotIdx = i / 2;
                feeder.LinkAdvancement(thisRound[nextSlotIdx], winnerToSideA: i % 2 == 0);
            }
            previousRoundIds = thisRound;
        }

        return matches;
    }

    private static IEnumerable<TournamentMatch> GenerateGroupRoundRobin(
        Guid tournamentId, List<TournamentParticipant> group, int groupNumber)
    {
        if (group.Count < 2) yield break;
        var list = new List<TournamentParticipant?>(group);
        if (list.Count % 2 != 0) list.Add(null);

        var teams = list.Count;
        var rounds = teams - 1;
        var halfSize = teams / 2;
        var rotation = Enumerable.Range(1, teams - 1).ToList();

        for (var r = 0; r < rounds; r++)
        {
            var matchNumber = 1;
            var positions = new List<int> { 0 };
            positions.AddRange(rotation);
            for (var i = 0; i < halfSize; i++)
            {
                var pa = list[positions[i]];
                var pb = list[positions[teams - 1 - i]];
                if (pa is null || pb is null) continue;
                var m = TournamentMatch.Create(tournamentId, BracketSide.GroupStage,
                                               round: r + 1, matchNumber: matchNumber++,
                                               groupNumber: groupNumber);
                m.SetParticipants(pa.Id, pb.Id);
                yield return m;
            }
            var last = rotation[^1];
            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(0, last);
        }
    }

    private static bool IsPowerOfTwo(int n) => n > 0 && (n & (n - 1)) == 0;
}
