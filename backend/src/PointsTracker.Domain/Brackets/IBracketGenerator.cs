using PointsTracker.Domain.Entities;

namespace PointsTracker.Domain.Brackets;

/// <summary>
/// Produces the full set of TournamentMatch slots (with advancement links
/// already wired up) for a given list of participants.
/// </summary>
public interface IBracketGenerator
{
    IReadOnlyList<TournamentMatch> Generate(Guid tournamentId, IReadOnlyList<TournamentParticipant> participants);
}
