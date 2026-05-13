using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

/// <summary>
/// Lazily spawn (or return) the Counter linked to a tournament match. The
/// counter inherits the tournament's sport + rule overrides; team names are
/// taken from the two participants. Slot must be Ready (both participants
/// known) before a counter can be opened.
/// </summary>
public record OpenMatchCounterCommand(
    Guid TournamentId,
    Guid MatchId,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<CounterDto>;

public class OpenMatchCounterHandler(
    ITournamentRepository tournaments,
    ICounterRepository counters,
    ITournamentAuthorizationService auth,
    ICounterMapper counterMapper
) : IRequestHandler<OpenMatchCounterCommand, CounterDto>
{
    public async Task<CounterDto> Handle(OpenMatchCounterCommand cmd, CancellationToken ct)
    {
        var t = await tournaments.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);
        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit) throw new ForbiddenException("Only the organiser can open a match counter.");

        var match = t.GetMatch(cmd.MatchId);

        if (match.CounterId is { } existing)
        {
            var existingCounter = await counters.GetByIdAsync(existing, ct)
                ?? throw new NotFoundException("Counter", existing);
            return counterMapper.ToDto(existingCounter, cmd.ActorUserId, null, null);
        }

        if (match.ParticipantAId is null || match.ParticipantBId is null)
            throw new DomainException("Match is waiting for participants from upstream slots.");

        var a = t.Participants.First(p => p.Id == match.ParticipantAId);
        var b = t.Participants.First(p => p.Id == match.ParticipantBId);

        // Per-stage rule overrides (final / semifinal) win over tournament-level
        // overrides when present. Keeps regular-stage matches on the default.
        var (matchRules, matchTimeoutsPerSet, matchTimeoutDuration) = t.ResolveMatchRules(match);

        var counter = Counter.Create(
            t.SportType,
            a.TeamName,
            b.TeamName,
            ownerUserId: t.OwnerUserId,
            sessionTokenHash: null,
            customRules: matchRules,
            indoorSwitchEverySets: t.IndoorSwitchEverySets,
            beachAutoSwitchSides: t.BeachAutoSwitchSides,
            customTimeoutsPerSet: matchTimeoutsPerSet,
            customTimeoutDurationSeconds: matchTimeoutDuration);

        await counters.AddAsync(counter, ct);
        t.AttachCounter(match.Id, counter.Id);
        counter.LinkToTournament(t.Id, match.Id, t.Name);
        // Save tournament + counter in the same SaveChanges so the FK is consistent.
        await tournaments.SaveChangesAsync(ct);

        return counterMapper.ToDto(counter, cmd.ActorUserId, null, null);
    }
}
