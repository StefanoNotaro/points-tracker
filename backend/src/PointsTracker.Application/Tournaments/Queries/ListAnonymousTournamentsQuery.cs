using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Queries;

public record ListAnonymousTournamentsQuery(IReadOnlyList<string> SessionTokens)
    : IRequest<IReadOnlyList<TournamentSummaryDto>>;

public class ListAnonymousTournamentsHandler(
    ITournamentRepository repo,
    IShareTokenService tokens
) : IRequestHandler<ListAnonymousTournamentsQuery, IReadOnlyList<TournamentSummaryDto>>
{
    public async Task<IReadOnlyList<TournamentSummaryDto>> Handle(
        ListAnonymousTournamentsQuery q, CancellationToken ct)
    {
        if (q.SessionTokens.Count == 0) return [];

        var hashes = q.SessionTokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(tokens.HashToken)
            .Distinct()
            .ToList();
        if (hashes.Count == 0) return [];

        var list = await repo.ListBySessionTokenHashesAsync(hashes, ct);

        return list
            .Select(t => new TournamentSummaryDto(
                t.Id,
                t.Name,
                t.SportType.ToString().ToLowerInvariant(),
                t.Format.ToString().ToLowerInvariant(),
                t.Status.ToString().ToLowerInvariant(),
                t.Participants.Count,
                t.CreatedAt,
                t.UpdatedAt))
            .ToList();
    }
}
