using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record DeleteCounterCommand(Guid CounterId, Guid? ActorUserId, string? SessionToken) : IRequest;

public class DeleteCounterHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService
) : IRequestHandler<DeleteCounterCommand>
{
    public async Task Handle(DeleteCounterCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, null);
        if (!access.IsOwner) throw new ForbiddenException("Only the owner can delete a counter.");

        counter.SoftDelete();
        await counterRepo.SaveChangesAsync(ct);
    }
}
