using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Brackets;

/// <summary>
/// Classic seeded knock-out. Participants are padded with byes to the next
/// power of two and arranged so that top seeds meet bottom seeds in round 1
/// (1 vs N, 2 vs N-1, ...). A bye is modelled as a slot with one null
/// participant and is auto-completed (walkover) at generation time.
/// </summary>
public sealed class SingleEliminationGenerator : IBracketGenerator
{
    private readonly BracketSide _side;

    public SingleEliminationGenerator(BracketSide side = BracketSide.Main) => _side = side;

    public IReadOnlyList<TournamentMatch> Generate(
        Guid tournamentId,
        IReadOnlyList<TournamentParticipant> participants)
    {
        if (participants.Count < 2)
            throw new DomainException("Single-elimination requires at least 2 participants.");

        var seeded = SeedingOrder(participants);
        var bracketSize = NextPowerOfTwo(seeded.Count);
        var seedPositions = StandardSeedPositions(bracketSize);

        // Build round 1 slots in bracket order
        var firstRoundIds = new Guid?[bracketSize / 2];
        var matches = new List<TournamentMatch>();

        for (var i = 0; i < bracketSize / 2; i++)
        {
            var seedA = seedPositions[i * 2];
            var seedB = seedPositions[i * 2 + 1];

            var participantA = seedA <= seeded.Count ? seeded[seedA - 1] : null;
            var participantB = seedB <= seeded.Count ? seeded[seedB - 1] : null;

            var m = TournamentMatch.Create(tournamentId, _side, round: 1, matchNumber: i + 1);
            m.SetParticipants(participantA?.Id, participantB?.Id);
            matches.Add(m);
            firstRoundIds[i] = m.Id;
        }

        // Build subsequent rounds and wire advancement
        var previousRound = firstRoundIds;
        var totalRounds = (int)Math.Log2(bracketSize);
        for (var r = 2; r <= totalRounds; r++)
        {
            var slots = previousRound.Length / 2;
            var thisRound = new Guid?[slots];
            for (var i = 0; i < slots; i++)
            {
                var m = TournamentMatch.Create(tournamentId, _side, round: r, matchNumber: i + 1);
                matches.Add(m);
                thisRound[i] = m.Id;
            }

            // Wire feeders
            for (var i = 0; i < previousRound.Length; i++)
            {
                var feederId = previousRound[i]!.Value;
                var nextSlotIdx = i / 2;
                var goesIntoA = i % 2 == 0;
                var feeder = matches.First(x => x.Id == feederId);
                feeder.LinkAdvancement(thisRound[nextSlotIdx], goesIntoA);
            }

            previousRound = thisRound;
        }

        // Auto-walkover any bye matches and propagate
        foreach (var m in matches.Where(m => m.RoundNumber == 1).ToList())
        {
            var hasA = m.ParticipantAId is not null;
            var hasB = m.ParticipantBId is not null;
            if (hasA ^ hasB)
            {
                var survivor = (m.ParticipantAId ?? m.ParticipantBId)!.Value;
                m.GrantWalkover(survivor);
                if (m.NextMatchId is { } nextId)
                {
                    var next = matches.First(x => x.Id == nextId);
                    var a = m.WinnerToSideA ? (Guid?)survivor : next.ParticipantAId;
                    var b = m.WinnerToSideA ? next.ParticipantBId : (Guid?)survivor;
                    next.SetParticipants(a, b);
                }
            }
        }

        return matches;
    }

    private static IReadOnlyList<TournamentParticipant> SeedingOrder(IReadOnlyList<TournamentParticipant> ps)
    {
        // Seeded participants first (in seed order), then unseeded in registration order.
        var seeded = ps.Where(p => p.Seed.HasValue).OrderBy(p => p.Seed!.Value).ToList();
        var rest   = ps.Where(p => !p.Seed.HasValue).OrderBy(p => p.RegisteredAt).ToList();
        return [..seeded, ..rest];
    }

    private static int NextPowerOfTwo(int n)
    {
        var p = 1;
        while (p < n) p *= 2;
        return p;
    }

    /// <summary>
    /// Standard bracket seed positions for a size N (N power of two):
    /// e.g. for size 8 returns [1,8,4,5,2,7,3,6].
    /// </summary>
    private static int[] StandardSeedPositions(int size)
    {
        int[] positions = [1, 2];
        while (positions.Length < size)
        {
            var next = positions.Length * 2;
            var doubled = new int[positions.Length * 2];
            for (var i = 0; i < positions.Length; i++)
            {
                doubled[i * 2]     = positions[i];
                doubled[i * 2 + 1] = next + 1 - positions[i];
            }
            positions = doubled;
        }
        return positions;
    }
}
