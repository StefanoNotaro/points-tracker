using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record CancelTimeoutCommand(
    Guid CounterId,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken
) : IRequest<CounterDto>;

public class CancelTimeoutHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<CancelTimeoutCommand, CounterDto>
{
    public async Task<CounterDto> Handle(CancelTimeoutCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        if (!access.CanEdit) throw new ForbiddenException();

        counter.CancelTimeout(cmd.ActorUserId);
        await counterRepo.SaveChangesAsync(ct);
        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
    }
}
