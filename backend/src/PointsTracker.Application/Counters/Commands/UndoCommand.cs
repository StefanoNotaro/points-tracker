using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record UndoCommand(
    Guid CounterId,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken,
    int Count = 1,
    string? ScorerToken = null
) : IRequest<CounterDto>;

public class UndoHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<UndoCommand, CounterDto>
{
    // Hard cap so a misbehaving client can't ask for a 10,000-step rewind.
    private const int MaxUndoSteps = 50;

    public async Task<CounterDto> Handle(UndoCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        var canScore = !access.CanEdit && await authService.HasScorerAccessAsync(counter, cmd.ScorerToken, ct);
        if (!access.CanEdit && !canScore) throw new ForbiddenException();

        var steps = Math.Clamp(cmd.Count, 1, MaxUndoSteps);
        for (var i = 0; i < steps && counter.CanUndo; i++)
            counter.Undo(cmd.ActorUserId);

        await counterRepo.SaveChangesAsync(ct);
        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken, canScore);
    }
}
