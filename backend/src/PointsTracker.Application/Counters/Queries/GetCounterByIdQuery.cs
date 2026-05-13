using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Queries;

public record GetCounterByIdQuery(
    Guid CounterId,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken
) : IRequest<CounterDto>;

public class GetCounterByIdHandler(
    ICounterRepository counterRepo,
    ITournamentRepository tournamentRepo,
    ICounterMapper mapper
) : IRequestHandler<GetCounterByIdQuery, CounterDto>
{
    public async Task<CounterDto> Handle(GetCounterByIdQuery query, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(query.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), query.CounterId);

        // Legacy backfill: counters spawned before the linkage columns existed
        // have null link fields. Look up the owning tournament once via the
        // tournament_matches index and populate so future reads are zero-query.
        if (counter.LinkedTournamentId is null)
        {
            var t = await tournamentRepo.GetByLinkedCounterAsync(counter.Id, ct);
            if (t is not null)
            {
                var match = t.Matches.FirstOrDefault(m => m.CounterId == counter.Id);
                if (match is not null)
                {
                    counter.LinkToTournament(t.Id, match.Id, t.Name);
                    await counterRepo.SaveChangesAsync(ct);
                }
            }
        }

        return mapper.ToDto(counter, query.ActorUserId, query.SessionToken, query.ShareToken);
    }
}
