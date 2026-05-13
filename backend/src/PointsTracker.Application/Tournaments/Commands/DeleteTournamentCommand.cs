using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

public record DeleteTournamentCommand(
    Guid TournamentId,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<Guid?>;

public class DeleteTournamentHandler(
    ITournamentRepository repo,
    ITournamentAuthorizationService auth
) : IRequestHandler<DeleteTournamentCommand, Guid?>
{
    public async Task<Guid?> Handle(DeleteTournamentCommand cmd, CancellationToken ct)
    {
        var t = await repo.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);
        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit) throw new ForbiddenException("You cannot delete this tournament.");

        var owner = t.OwnerUserId;
        t.SoftDelete();
        await repo.SaveChangesAsync(ct);
        return owner;
    }
}
