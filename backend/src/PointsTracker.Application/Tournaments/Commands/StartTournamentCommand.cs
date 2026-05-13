using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Brackets;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

public record StartTournamentCommand(
    Guid TournamentId,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<TournamentDto>;

public class StartTournamentHandler(
    ITournamentRepository repo,
    ITournamentAuthorizationService auth,
    ITournamentMapper mapper
) : IRequestHandler<StartTournamentCommand, TournamentDto>
{
    public async Task<TournamentDto> Handle(StartTournamentCommand cmd, CancellationToken ct)
    {
        var t = await repo.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);
        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit) throw new ForbiddenException("You cannot start this tournament.");

        var generator = BracketGeneratorFactory.For(
            t.Format,
            groupCount: t.GroupCount ?? 2,
            advancePerGroup: t.AdvancePerGroup ?? 2);
        var matches   = generator.Generate(t.Id, t.Participants);
        t.StartWithBracket(matches);
        // The matches were created in-memory with client-assigned Ids (needed
        // so the bracket generator can wire NextMatchId pointers). Adding
        // them to the tracked tournament's navigation would default to EF's
        // "Modified" state — flip them to Added so SaveChanges INSERTs.
        repo.TrackAsNew(matches);

        await repo.SaveChangesAsync(ct);
        return mapper.ToDto(t, cmd.ActorUserId, cmd.ActorSessionToken);
    }
}
