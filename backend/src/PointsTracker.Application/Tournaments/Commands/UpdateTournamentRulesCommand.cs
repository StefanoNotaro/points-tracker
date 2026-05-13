using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.Commands;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

/// <summary>
/// Single PATCH-style update that lets the organiser change any combination
/// of: name, schedule, and rule overrides. Null fields are ignored — only
/// fields the caller provides get touched. Rules update applies to FUTURE
/// matches only (in-progress/completed matches keep their rules).
/// </summary>
public record UpdateTournamentRulesCommand(
    Guid TournamentId,
    string? Name,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool ClearStartsAt,
    bool ClearEndsAt,
    CustomRulesInput? CustomRules,
    bool ClearCustomRules,
    int? IndoorSwitchEverySets,
    bool BeachAutoSwitchSides,
    int? CustomTimeoutsPerSet,
    int? CustomTimeoutDurationSeconds,
    CustomRulesInput? FinalRules,
    int? FinalTimeoutsPerSet,
    int? FinalTimeoutDurationSeconds,
    bool ClearFinalRules,
    CustomRulesInput? SemifinalRules,
    int? SemifinalTimeoutsPerSet,
    int? SemifinalTimeoutDurationSeconds,
    bool ClearSemifinalRules,
    Guid? ActorUserId,
    string? ActorSessionToken
) : IRequest<TournamentDto>;

public class UpdateTournamentRulesValidator : AbstractValidator<UpdateTournamentRulesCommand>
{
    public UpdateTournamentRulesValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.IndoorSwitchEverySets)
            .Must(v => v is null or 1 or 2).WithMessage("Indoor interval must be 1 or 2.");
        RuleFor(x => x.CustomTimeoutsPerSet)
            .InclusiveBetween(0, 9).When(x => x.CustomTimeoutsPerSet is not null);
        RuleFor(x => x.CustomTimeoutDurationSeconds)
            .InclusiveBetween(5, 600).When(x => x.CustomTimeoutDurationSeconds is not null);
        When(x => x.CustomRules is not null, () =>
        {
            RuleFor(x => x.CustomRules!.PointsPerSet).InclusiveBetween(1, 99);
            RuleFor(x => x.CustomRules!.LastSetPoints).InclusiveBetween(1, 99);
            RuleFor(x => x.CustomRules!.SetsToWin).InclusiveBetween(1, 9);
            RuleFor(x => x.CustomRules!.TotalSets).InclusiveBetween(1, 9);
            RuleFor(x => x.CustomRules!)
                .Must(r => r.SetsToWin <= r.TotalSets)
                .WithMessage("SetsToWin cannot exceed TotalSets.");
        });
    }
}

public class UpdateTournamentRulesHandler(
    ITournamentRepository repo,
    ICounterRepository counters,
    ITournamentAuthorizationService auth,
    ITournamentMapper mapper
) : IRequestHandler<UpdateTournamentRulesCommand, TournamentDto>
{
    public async Task<TournamentDto> Handle(UpdateTournamentRulesCommand cmd, CancellationToken ct)
    {
        var t = await repo.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);
        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit) throw new ForbiddenException("You cannot modify this tournament.");

        if (!string.IsNullOrWhiteSpace(cmd.Name) && cmd.Name.Trim() != t.Name)
        {
            t.Rename(cmd.Name);
            // Keep denormalised tournament name on linked counters in sync.
            var linked = await counters.ListByTournamentAsync(t.Id, ct);
            foreach (var c in linked) c.UpdateLinkedTournamentName(t.Name);
        }

        if (cmd.StartsAt.HasValue || cmd.EndsAt.HasValue || cmd.ClearStartsAt || cmd.ClearEndsAt)
        {
            var newStart = cmd.ClearStartsAt ? null : (cmd.StartsAt ?? t.StartsAt);
            var newEnd   = cmd.ClearEndsAt   ? null : (cmd.EndsAt   ?? t.EndsAt);
            t.Schedule(newStart, newEnd);
        }

        // Rules: explicit clear via ClearCustomRules, explicit set via CustomRules,
        // otherwise leave existing customs in place.
        SportRules? rules = cmd.ClearCustomRules
            ? null
            : (cmd.CustomRules is null
                ? t.CurrentCustomRules
                : new SportRules(
                    cmd.CustomRules.PointsPerSet,
                    cmd.CustomRules.LastSetPoints,
                    cmd.CustomRules.SetsToWin,
                    cmd.CustomRules.TotalSets,
                    cmd.CustomRules.WinByTwo));

        // The other rule overrides are not nullable on the API; we pass them through
        // and trust the caller to send the desired state.
        t.UpdateRules(rules, cmd.IndoorSwitchEverySets, cmd.BeachAutoSwitchSides,
                      cmd.CustomTimeoutsPerSet, cmd.CustomTimeoutDurationSeconds);

        SportRules? finalR = cmd.ClearFinalRules
            ? null
            : (cmd.FinalRules is null ? CurrentFinal(t) : ToSportRules(cmd.FinalRules));
        SportRules? semiR = cmd.ClearSemifinalRules
            ? null
            : (cmd.SemifinalRules is null ? CurrentSemi(t) : ToSportRules(cmd.SemifinalRules));

        t.UpdateStageRules(
            finalR, cmd.FinalTimeoutsPerSet, cmd.FinalTimeoutDurationSeconds,
            semiR, cmd.SemifinalTimeoutsPerSet, cmd.SemifinalTimeoutDurationSeconds);

        await repo.SaveChangesAsync(ct);
        return mapper.ToDto(t, cmd.ActorUserId, cmd.ActorSessionToken);
    }

    private static SportRules ToSportRules(CustomRulesInput x) =>
        new(x.PointsPerSet, x.LastSetPoints, x.SetsToWin, x.TotalSets, x.WinByTwo);

    private static SportRules? CurrentFinal(Tournament t) =>
        t.FinalPointsPerSet.HasValue && t.FinalLastSetPoints.HasValue
        && t.FinalSetsToWin.HasValue && t.FinalTotalSets.HasValue && t.FinalWinByTwo.HasValue
            ? new SportRules(t.FinalPointsPerSet.Value, t.FinalLastSetPoints.Value,
                t.FinalSetsToWin.Value, t.FinalTotalSets.Value, t.FinalWinByTwo.Value)
            : null;
    private static SportRules? CurrentSemi(Tournament t) =>
        t.SemifinalPointsPerSet.HasValue && t.SemifinalLastSetPoints.HasValue
        && t.SemifinalSetsToWin.HasValue && t.SemifinalTotalSets.HasValue && t.SemifinalWinByTwo.HasValue
            ? new SportRules(t.SemifinalPointsPerSet.Value, t.SemifinalLastSetPoints.Value,
                t.SemifinalSetsToWin.Value, t.SemifinalTotalSets.Value, t.SemifinalWinByTwo.Value)
            : null;
}
