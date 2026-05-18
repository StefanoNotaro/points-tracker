using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Queries;

public record ScorerJoinDto(Guid? CounterId, Guid TournamentId, Guid MatchId);

public record ResolveMatchScorerLinkQuery(string RawToken) : IRequest<ScorerJoinDto>;

public class ResolveMatchScorerLinkHandler(
    IMatchScorerLinkRepository scorerLinks,
    ITournamentRepository tournaments,
    IShareTokenService tokenService
) : IRequestHandler<ResolveMatchScorerLinkQuery, ScorerJoinDto>
{
    public async Task<ScorerJoinDto> Handle(ResolveMatchScorerLinkQuery query, CancellationToken ct)
    {
        var hash = tokenService.HashToken(query.RawToken);
        var link = await scorerLinks.GetByTokenHashAsync(hash, ct);

        if (link is null || !link.IsActive)
            throw new NotFoundException("MatchScorerLink", query.RawToken);

        // Load the tournament to find the counter linked to this match (if opened).
        var tournament = await tournaments.GetByIdAsync(link.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", link.TournamentId);

        var match = tournament.Matches.FirstOrDefault(m => m.Id == link.MatchId)
            ?? throw new NotFoundException("TournamentMatch", link.MatchId);

        return new ScorerJoinDto(match.CounterId, link.TournamentId, link.MatchId);
    }
}
