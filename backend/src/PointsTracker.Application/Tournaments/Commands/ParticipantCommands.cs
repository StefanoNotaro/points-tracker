using FluentValidation;
using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

public record AddParticipantCommand(
    Guid TournamentId,
    string TeamName,
    int? Seed,
    Guid? UserId,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<TournamentDto>;

public class AddParticipantValidator : AbstractValidator<AddParticipantCommand>
{
    public AddParticipantValidator()
    {
        RuleFor(x => x.TeamName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Seed).GreaterThan(0).When(x => x.Seed.HasValue);
    }
}

public class AddParticipantHandler(
    ITournamentRepository repo,
    ITournamentAuthorizationService auth,
    ITournamentMapper mapper
) : IRequestHandler<AddParticipantCommand, TournamentDto>
{
    public async Task<TournamentDto> Handle(AddParticipantCommand cmd, CancellationToken ct)
    {
        var t = await repo.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);
        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit) throw new ForbiddenException("You cannot modify this tournament.");

        t.AddParticipant(cmd.TeamName, cmd.Seed, cmd.UserId);
        await repo.SaveChangesAsync(ct);
        return mapper.ToDto(t, cmd.ActorUserId, cmd.ActorSessionToken);
    }
}

public record RemoveParticipantCommand(
    Guid TournamentId,
    Guid ParticipantId,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<TournamentDto>;

public class RemoveParticipantHandler(
    ITournamentRepository repo,
    ITournamentAuthorizationService auth,
    ITournamentMapper mapper
) : IRequestHandler<RemoveParticipantCommand, TournamentDto>
{
    public async Task<TournamentDto> Handle(RemoveParticipantCommand cmd, CancellationToken ct)
    {
        var t = await repo.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);
        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit) throw new ForbiddenException("You cannot modify this tournament.");

        t.RemoveParticipant(cmd.ParticipantId);
        await repo.SaveChangesAsync(ct);
        return mapper.ToDto(t, cmd.ActorUserId, cmd.ActorSessionToken);
    }
}
