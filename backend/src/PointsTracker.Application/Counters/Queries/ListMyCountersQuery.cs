using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Queries;

public record ListMyCountersQuery(Guid OwnerUserId) : IRequest<IReadOnlyList<CounterSummaryDto>>;

public class ListMyCountersHandler(ICounterRepository counterRepo)
    : IRequestHandler<ListMyCountersQuery, IReadOnlyList<CounterSummaryDto>>
{
    public async Task<IReadOnlyList<CounterSummaryDto>> Handle(ListMyCountersQuery query, CancellationToken ct)
    {
        var counters = await counterRepo.ListByOwnerAsync(query.OwnerUserId, ct);

        return counters
            .Select(c => new CounterSummaryDto(
                c.Id,
                c.SportType.ToString().ToLowerInvariant(),
                c.TeamAName,
                c.TeamBName,
                c.Status.ToString().ToLowerInvariant(),
                c.SetsWonA,
                c.SetsWonB,
                c.CurrentScoreA,
                c.CurrentScoreB,
                c.CreatedAt,
                c.UpdatedAt))
            .ToList();
    }
}
