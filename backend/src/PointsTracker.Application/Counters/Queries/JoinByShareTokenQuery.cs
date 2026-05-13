using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Queries;

public record JoinByShareTokenQuery(string Token, Guid? ActorUserId) : IRequest<CounterDto>;

public class JoinByShareTokenHandler(
    IShareTokenRepository shareTokenRepo,
    ICounterRepository counterRepo,
    ICounterMapper mapper
) : IRequestHandler<JoinByShareTokenQuery, CounterDto>
{
    public async Task<CounterDto> Handle(JoinByShareTokenQuery query, CancellationToken ct)
    {
        var token = await shareTokenRepo.GetByTokenAsync(query.Token, ct);
        if (token is null || !token.IsValid)
            throw new NotFoundException("ShareToken", query.Token);

        var counter = await counterRepo.GetByIdAsync(token.CounterId, ct)
            ?? throw new NotFoundException("Counter", token.CounterId);

        return mapper.ToDto(counter, query.ActorUserId, sessionToken: null, shareToken: query.Token);
    }
}
