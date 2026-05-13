using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Queries;

public record GetTournamentByIdQuery(
    Guid TournamentId,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<TournamentDto>;

public class GetTournamentByIdHandler(
    ITournamentRepository tournaments,
    ICounterRepository counters,
    ITournamentMapper mapper
) : IRequestHandler<GetTournamentByIdQuery, TournamentDto>
{
    public async Task<TournamentDto> Handle(GetTournamentByIdQuery q, CancellationToken ct)
    {
        var t = await tournaments.GetByIdAsync(q.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", q.TournamentId);

        // Lazy reconcile fallback. The TournamentLiveBridge already records
        // results in real time as counters finish, so most GETs do zero
        // work here. We still run a defensive pass in one batched query
        // for counters changed before the bridge existed (legacy data) or
        // when the bridge failed mid-broadcast.
        var pending = t.Matches
            .Where(m => m.Status == TournamentMatchStatus.InProgress && m.CounterId.HasValue)
            .ToList();
        if (pending.Count > 0)
        {
            var ids = pending.Select(m => m.CounterId!.Value).ToList();
            var snapshot = await counters.ListByIdsAsync(ids, ct);
            var byId = snapshot.ToDictionary(c => c.Id);

            var reconciled = false;
            foreach (var m in pending)
            {
                if (!byId.TryGetValue(m.CounterId!.Value, out var counter)) continue;
                if (counter.Status != CounterStatus.Finished) continue;
                var winnerId = counter.SetsWonA > counter.SetsWonB
                    ? m.ParticipantAId
                    : m.ParticipantBId;
                if (winnerId is null) continue;

                t.RecordMatchResult(m.Id, winnerId.Value);
                reconciled = true;
            }
            if (reconciled) await tournaments.SaveChangesAsync(ct);
        }

        return mapper.ToDto(t, q.ActorUserId, q.ActorSessionToken);
    }
}
