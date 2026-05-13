using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

public record RecordMatchResultCommand(
    Guid TournamentId,
    Guid MatchId,
    Guid WinnerParticipantId,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<TournamentDto>;

public class RecordMatchResultHandler(
    ITournamentRepository repo,
    ITournamentAuthorizationService auth,
    ITournamentMapper mapper
) : IRequestHandler<RecordMatchResultCommand, TournamentDto>
{
    public async Task<TournamentDto> Handle(RecordMatchResultCommand cmd, CancellationToken ct)
    {
        var t = await repo.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);
        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit) throw new ForbiddenException("Only the organiser can record a result.");

        t.RecordMatchResult(cmd.MatchId, cmd.WinnerParticipantId);
        await repo.SaveChangesAsync(ct);
        return mapper.ToDto(t, cmd.ActorUserId, cmd.ActorSessionToken);
    }
}
