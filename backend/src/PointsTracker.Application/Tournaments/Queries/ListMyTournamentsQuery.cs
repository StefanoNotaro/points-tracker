using MediatR;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Queries;

public record ListMyTournamentsQuery(Guid OwnerUserId) : IRequest<IReadOnlyList<TournamentSummaryDto>>;

public class ListMyTournamentsHandler(ITournamentRepository repo)
    : IRequestHandler<ListMyTournamentsQuery, IReadOnlyList<TournamentSummaryDto>>
{
    public async Task<IReadOnlyList<TournamentSummaryDto>> Handle(ListMyTournamentsQuery q, CancellationToken ct)
    {
        var list = await repo.ListByOwnerAsync(q.OwnerUserId, ct);
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
