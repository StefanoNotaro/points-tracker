using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record RedoCommand(
    Guid CounterId,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken,
    int Count = 1,
    string? ScorerToken = null
) : IRequest<CounterDto>;

public class RedoHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<RedoCommand, CounterDto>
{
    private const int MaxRedoSteps = 50;

    public async Task<CounterDto> Handle(RedoCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        var canScore = !access.CanEdit && await authService.HasScorerAccessAsync(counter, cmd.ScorerToken, ct);
        if (!access.CanEdit && !canScore) throw new ForbiddenException();

        var steps = Math.Clamp(cmd.Count, 1, MaxRedoSteps);
        for (var i = 0; i < steps && counter.CanRedo; i++)
            counter.Redo(cmd.ActorUserId);

        await counterRepo.SaveChangesAsync(ct);
        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken, canScore);
    }
}
