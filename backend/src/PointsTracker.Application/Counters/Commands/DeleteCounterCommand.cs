using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

/// <summary>
/// Soft-deletes a counter. Returns the owner user id (if any) so the API
/// layer can fan out a SignalR notification to the owner's dashboard.
/// </summary>
public record DeleteCounterCommand(Guid CounterId, Guid? ActorUserId, string? SessionToken) : IRequest<Guid?>;

public class DeleteCounterHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService
) : IRequestHandler<DeleteCounterCommand, Guid?>
{
    public async Task<Guid?> Handle(DeleteCounterCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, null);
        if (!access.IsOwner) throw new ForbiddenException("Only the owner can delete a counter.");

        var ownerId = counter.OwnerUserId;
        counter.SoftDelete();
        await counterRepo.SaveChangesAsync(ct);
        return ownerId;
    }
}
