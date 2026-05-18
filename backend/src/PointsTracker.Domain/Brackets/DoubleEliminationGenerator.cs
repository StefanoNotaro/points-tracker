using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;

namespace PointsTracker.Domain.Brackets;

/// <summary>
/// Double elimination: winners bracket = single-elim; losers bracket
/// receives each round's losers and itself is single-elim. The two bracket
/// winners meet in a Grand Final.
///
/// Non-power-of-two team counts are handled gracefully: the winners bracket
/// pads to the next power of two using byes (walkovers). Bye slots produce
/// no loser, so any losers-bracket slot whose both WR1 feeders are byes is
/// immediately marked as a structural walkover (GrantDoubleBye). At runtime,
/// Tournament.TryAutoWalkover detects any LB slot that receives only one
/// participant (the other feeder was a bye) and auto-advances it.
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
        // The LB alternates between two round types:
        //   Feed round (odd LR, e.g. LR1, LR3): LR winners pair up → count halves → 2:1 mapping, alternating sides.
        //   Drop-in round (even LR, e.g. LR2, LR4): each LR winner faces a WR loser → same count → 1:1 mapping, all to side A.
        for (var lr = 0; lr < losersByRound.Count - 1; lr++)
        {
            var current = losersByRound[lr];
            var next    = losersByRound[lr + 1];
            var dropIn  = current.Count == next.Count; // feed→drop-in transition
            for (var i = 0; i < current.Count; i++)
            {
                var nextIdx  = dropIn ? i : i / 2;
                var toSideA  = dropIn || i % 2 == 0; // drop-in: LR winners always claim A; WR losers fill B
                current[i].LinkAdvancement(next[nextIdx].Id, winnerToSideA: toSideA);
            }
        }

        // 5. Mark LR1 slots whose both WR1 feeders are already walkovers (byes):
        // no loser will ever arrive, so the slot is structurally dead and must be
        // resolved now so downstream TryAutoWalkover checks don't treat it as pending.
        foreach (var lr1Match in losersByRound[0])
        {
            var feeders = winners
                .Where(w => w.RoundNumber == 1 && w.NextLoserMatchId == lr1Match.Id)
                .ToList();
            if (feeders.Count > 0 && feeders.All(f => f.Status == TournamentMatchStatus.Walkover))
                lr1Match.GrantDoubleBye();
        }

        // 6. Grand Final: winner of last losers round vs winner of last winners round.
        var grand = TournamentMatch.Create(tournamentId, BracketSide.GrandFinal, round: 1, matchNumber: 1);
        matches.Add(grand);

        var winnersFinal = winnerRounds[^1].Single();
        // Preserve the NextLoserMatchId wired in step 3 (winners-final loser drops into LB final).
        // Calling LinkAdvancement without it would default to null and erase that link.
        winnersFinal.LinkAdvancement(
            grand.Id, winnerToSideA: true,
            nextLoserMatchId: winnersFinal.NextLoserMatchId,
            loserToSideA: winnersFinal.LoserToSideA);

        var losersFinal = losersByRound[^1].Single();
        losersFinal.LinkAdvancement(grand.Id, winnerToSideA: false);

        return matches;
    }
}
