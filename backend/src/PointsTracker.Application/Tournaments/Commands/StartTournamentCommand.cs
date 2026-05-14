using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Brackets;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

public record StartTournamentCommand(
    Guid TournamentId,
    Guid? ActorUserId,
    string? ActorSessionToken,
    bool RandomizeUnseeded = false
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

        // Apply ordering: seeded teams keep their seed value (the generators
        // sort by seed). For unseeded teams, default to registration order,
        // or shuffle them when the caller asked to randomize.
        var participants = OrderForBracket(t.Participants, cmd.RandomizeUnseeded);

        var generator = BracketGeneratorFactory.For(
            t.Format,
            groupCount: t.GroupCount ?? 2,
            advancePerGroup: t.AdvancePerGroup ?? 2);
        var matches   = generator.Generate(t.Id, participants);
        t.StartWithBracket(matches);
        repo.TrackAsNew(matches);

        await repo.SaveChangesAsync(ct);
        return mapper.ToDto(t, cmd.ActorUserId, cmd.ActorSessionToken);
    }

    private static IReadOnlyList<TournamentParticipant> OrderForBracket(
        IReadOnlyList<TournamentParticipant> participants,
        bool randomizeUnseeded)
    {
        var seeded   = participants.Where(p => p.Seed.HasValue).ToList();
        var unseeded = participants.Where(p => !p.Seed.HasValue).ToList();
        if (randomizeUnseeded)
        {
            var rng = Random.Shared;
            for (var i = unseeded.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (unseeded[i], unseeded[j]) = (unseeded[j], unseeded[i]);
            }
        }
        else
        {
            unseeded = unseeded.OrderBy(p => p.RegisteredAt).ToList();
        }
        return [..seeded, ..unseeded];
    }
}
