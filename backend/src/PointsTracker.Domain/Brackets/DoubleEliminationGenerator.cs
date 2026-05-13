using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Brackets;

/// <summary>
/// Double elimination: winners bracket = single-elim; losers bracket
/// receives each round's losers and itself is single-elim. The two bracket
/// winners meet in a Grand Final.
///
/// To keep complexity in check this generator requires the participant
/// count to be a power of two (pad with byes if needed at the participant
/// management UI). For non-power-of-two we fall back to filling byes
/// in the winners side; the losers bracket is unchanged in shape.
/// </summary>
public sealed class DoubleEliminationGenerator : IBracketGenerator
{
    public IReadOnlyList<TournamentMatch> Generate(
        Guid tournamentId,
        IReadOnlyList<TournamentParticipant> participants)
    {
        if (participants.Count < 4)
            throw new DomainException("Double elimination requires at least 4 participants.");

        // 1. Winners bracket via the single-elim generator (tagged as Winners side)
        var winners = new SingleEliminationGenerator(BracketSide.Winners)
            .Generate(tournamentId, participants).ToList();

        var winnerRounds = winners.GroupBy(m => m.RoundNumber)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(m => m.MatchNumber).ToList())
            .ToList();

        // 2. Losers bracket: classic interleaved structure.
        // Losers bracket has 2 * (winnerRounds.Count - 1) rounds.
        var matches = new List<TournamentMatch>(winners);
        var losersByRound = new List<List<TournamentMatch>>();

        var firstWinnerRoundLosers = winnerRounds[0].Count;
        var loserRoundCount = 2 * (winnerRounds.Count - 1);

        // Pre-create losers rounds with the correct slot counts.
        // Pattern: L1 = WR1.Count/2, L2 = WR1.Count/2, L3 = previous/2 etc., with
        // drop-ins from the next winner round on even losers rounds.
        var currentSlots = firstWinnerRoundLosers / 2;
        for (var lr = 1; lr <= loserRoundCount; lr++)
        {
            var slots = currentSlots;
            var round = new List<TournamentMatch>();
            for (var i = 0; i < slots; i++)
            {
                var m = TournamentMatch.Create(tournamentId, BracketSide.Losers, round: lr, matchNumber: i + 1);
                round.Add(m);
                matches.Add(m);
            }
            losersByRound.Add(round);

            // Halve slots only on odd-indexed (1-based) losers rounds (after drop-in rounds).
            if (lr % 2 == 0) currentSlots = Math.Max(1, currentSlots / 2);
        }

        // 3. Wire winner-bracket losers into the losers bracket.
        // WR1 losers → LR1 (paired); WR_n losers (n>=2) → LR_{2(n-1)}.
        for (var i = 0; i < winnerRounds[0].Count; i++)
        {
            var wm = winnerRounds[0][i];
            var lr1Idx = i / 2;
            var toSideA = i % 2 == 0;
            wm.LinkAdvancement(
                nextMatchId: wm.NextMatchId,           // winner path unchanged
                winnerToSideA: wm.WinnerToSideA,
                nextLoserMatchId: losersByRound[0][lr1Idx].Id,
                loserToSideA: toSideA);
        }
        for (var wr = 1; wr < winnerRounds.Count; wr++)
        {
            var loserRoundIdx = 2 * wr - 1; // 0-based index into losersByRound
            if (loserRoundIdx >= losersByRound.Count) break;
            for (var i = 0; i < winnerRounds[wr].Count; i++)
            {
                var wm = winnerRounds[wr][i];
                wm.LinkAdvancement(
                    nextMatchId: wm.NextMatchId,
                    winnerToSideA: wm.WinnerToSideA,
                    nextLoserMatchId: losersByRound[loserRoundIdx][i].Id,
                    loserToSideA: false /* drop-ins fill B slots */);
            }
        }

        // 4. Wire losers-bracket advancement (each LR_n winner → LR_{n+1}).
        for (var lr = 0; lr < losersByRound.Count - 1; lr++)
        {
            var current = losersByRound[lr];
            var next = losersByRound[lr + 1];
            for (var i = 0; i < current.Count; i++)
            {
                var nextIdx = i / 2;
                if (nextIdx >= next.Count) nextIdx = next.Count - 1;
                current[i].LinkAdvancement(next[nextIdx].Id, winnerToSideA: i % 2 == 0);
            }
        }

        // 5. Grand Final: winner of last losers round vs winner of last winners round.
        var grand = TournamentMatch.Create(tournamentId, BracketSide.GrandFinal, round: 1, matchNumber: 1);
        matches.Add(grand);

        var winnersFinal = winnerRounds[^1].Single();
        winnersFinal.LinkAdvancement(grand.Id, winnerToSideA: true);

        var losersFinal = losersByRound[^1].Single();
        losersFinal.LinkAdvancement(grand.Id, winnerToSideA: false);

        return matches;
    }
}
