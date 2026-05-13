using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Brackets;

/// <summary>
/// Every participant plays every other once, scheduled in rounds via the
/// circle method (https://en.wikipedia.org/wiki/Round-robin_tournament#Scheduling_algorithm).
/// For odd participant counts one team gets a bye each round.
/// </summary>
public sealed class RoundRobinGenerator : IBracketGenerator
{
    public IReadOnlyList<TournamentMatch> Generate(
        Guid tournamentId,
        IReadOnlyList<TournamentParticipant> participants)
    {
        if (participants.Count < 2)
            throw new DomainException("Round robin requires at least 2 participants.");

        var list = participants.OrderBy(p => p.RegisteredAt).ToList();
        var n = list.Count;
        var hasBye = n % 2 != 0;
        if (hasBye) list.Add(null!); // bye slot

        var teams = list.Count;
        var rounds = teams - 1;
        var halfSize = teams / 2;
        var matches = new List<TournamentMatch>();

        // Fixed first participant, rotate the rest.
        var rotation = Enumerable.Range(1, teams - 1).ToList();

        for (var r = 0; r < rounds; r++)
        {
            var matchNumber = 1;
            // Standard circle method: positions 0..teams-1, position 0 fixed, others rotate.
            var positions = new List<int> { 0 };
            positions.AddRange(rotation);
            for (var i = 0; i < halfSize; i++)
            {
                var a = positions[i];
                var b = positions[teams - 1 - i];
                var pa = list[a];
                var pb = list[b];
                if (pa is null || pb is null) continue; // bye pairing, skip

                var m = TournamentMatch.Create(tournamentId, BracketSide.Main, round: r + 1, matchNumber: matchNumber++);
                m.SetParticipants(pa.Id, pb.Id);
                matches.Add(m);
            }

            // Rotate everything except position 0
            var last = rotation[^1];
            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(0, last);
        }

        return matches;
    }
}
