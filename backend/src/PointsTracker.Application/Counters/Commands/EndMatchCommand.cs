using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record EndMatchCommand(
    Guid CounterId,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken
) : IRequest<CounterDto>;

public class EndMatchHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<EndMatchCommand, CounterDto>
{
    public async Task<CounterDto> Handle(EndMatchCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        if (!access.CanEdit) throw new ForbiddenException();

        counter.EndMatchManually(cmd.ActorUserId);
        await counterRepo.SaveChangesAsync(ct);

        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
    }
}
