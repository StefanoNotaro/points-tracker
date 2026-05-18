using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Queries;

public record ListMatchScorerLinksQuery(
    Guid TournamentId,
    Guid MatchId,
    Guid ActorUserId,
    string? ActorSessionToken
) : IRequest<IReadOnlyList<MatchScorerLinkDto>>;

public class ListMatchScorerLinksHandler(
    ITournamentRepository tournaments,
    IMatchScorerLinkRepository scorerLinks,
    ITournamentAuthorizationService auth
) : IRequestHandler<ListMatchScorerLinksQuery, IReadOnlyList<MatchScorerLinkDto>>
{
    public async Task<IReadOnlyList<MatchScorerLinkDto>> Handle(ListMatchScorerLinksQuery query, CancellationToken ct)
    {
        var t = await tournaments.GetByIdAsync(query.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", query.TournamentId);

        var access = auth.GetAccess(t, query.ActorUserId, query.ActorSessionToken);
        if (!access.CanEdit)
            throw new ForbiddenException("Only the tournament organiser can view scorer links.");

        // Verify the match belongs to this tournament.
        t.GetMatch(query.MatchId);

        var links = await scorerLinks.GetByMatchIdAsync(query.MatchId, ct);
        return links.Select(l => new MatchScorerLinkDto(
            l.Id,
            l.TournamentId,
            l.MatchId,
            l.Label,
            l.GrantedToUserId,
            l.IsActive,
            l.CreatedAt)).ToList();
    }
}
