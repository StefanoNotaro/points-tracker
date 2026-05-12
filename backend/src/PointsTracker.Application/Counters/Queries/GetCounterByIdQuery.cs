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
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<GetCounterByIdQuery, CounterDto>
{
    public async Task<CounterDto> Handle(GetCounterByIdQuery query, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(query.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), query.CounterId);

        var access = authService.GetAccess(counter, query.ActorUserId, query.SessionToken, query.ShareToken);
        return mapper.ToDto(counter, query.ActorUserId, query.ShareToken);
    }
}
