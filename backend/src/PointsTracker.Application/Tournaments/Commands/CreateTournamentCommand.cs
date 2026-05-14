using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.Commands;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

public record CreateTournamentCommand(
    string Name,
    string SportType,
    string Format,
    Guid? ActorUserId,
    string? ActorSessionToken,
    CustomRulesInput? CustomRules,
    int? IndoorSwitchEverySets = null,
    bool BeachAutoSwitchSides = true,
    int? CustomTimeoutsPerSet = null,
    int? CustomTimeoutDurationSeconds = null,
    int? GroupCount = null,
    int? AdvancePerGroup = null
) : IRequest<CreateTournamentResponseDto>;

public class CreateTournamentValidator : AbstractValidator<CreateTournamentCommand>
{
    public CreateTournamentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SportType).NotEmpty().Must(v => Enum.TryParse<SportType>(v, true, out _))
            .WithMessage("Invalid sport type.");
        RuleFor(x => x.Format).NotEmpty().Must(v => Enum.TryParse<TournamentFormat>(v, true, out _))
            .WithMessage("Invalid tournament format.");

        When(x => string.Equals(x.SportType, "custom", StringComparison.OrdinalIgnoreCase), () =>
            RuleFor(x => x.CustomRules).NotNull().WithMessage("Custom sport requires rules."));

        RuleFor(x => x.IndoorSwitchEverySets)
            .Must(v => v is null or 1 or 2).WithMessage("Indoor side-switch interval must be 1 or 2.");
        RuleFor(x => x.CustomTimeoutsPerSet)
            .InclusiveBetween(0, 9).When(x => x.CustomTimeoutsPerSet is not null);
        RuleFor(x => x.CustomTimeoutDurationSeconds)
            .InclusiveBetween(5, 600).When(x => x.CustomTimeoutDurationSeconds is not null);

        When(x => string.Equals(x.Format, nameof(TournamentFormat.GroupStageElimination), StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.GroupCount).InclusiveBetween(2, 8)
                .WithMessage("Group count must be between 2 and 8.");
            RuleFor(x => x.AdvancePerGroup).InclusiveBetween(1, 4)
                .WithMessage("Advance-per-group must be between 1 and 4.");
            RuleFor(x => x).Must(c =>
            {
                var slots = (c.GroupCount ?? 2) * (c.AdvancePerGroup ?? 2);
                return slots >= 2 && (slots & (slots - 1)) == 0;
            }).WithMessage("Groups × advance-per-group must be a power of two (e.g. 2×2, 2×4, 4×2).");
        });
    }
}

public class CreateTournamentHandler(
    ITournamentRepository repo,
    IShareTokenService tokens,
    ITournamentMapper mapper
) : IRequestHandler<CreateTournamentCommand, CreateTournamentResponseDto>
{
    public async Task<CreateTournamentResponseDto> Handle(CreateTournamentCommand cmd, CancellationToken ct)
    {
        var sport  = Enum.Parse<SportType>(cmd.SportType, true);
        var format = Enum.Parse<TournamentFormat>(cmd.Format, true);

        string? rawToken = null;
        string? tokenHash = null;
        if (cmd.ActorUserId is null)
        {
            // Anonymous users may only have ONE active tournament at a time.
            var existingHash = string.IsNullOrEmpty(cmd.ActorSessionToken)
                ? null
                : tokens.HashToken(cmd.ActorSessionToken);
            if (existingHash is not null)
            {
                var active = await repo.GetActiveAnonymousAsync(existingHash, ct);
                if (active is not null)
                    throw new DomainException("Anonymous users may only have one active tournament. Finish or delete the existing one first.");
            }

            rawToken  = tokens.GenerateSessionToken();
            tokenHash = tokens.HashToken(rawToken);
        }

        SportRules? customRules = cmd.CustomRules is null ? null : new SportRules(
            cmd.CustomRules.PointsPerSet,
            cmd.CustomRules.LastSetPoints,
            cmd.CustomRules.SetsToWin,
            cmd.CustomRules.TotalSets,
            cmd.CustomRules.WinByTwo);

        var t = Tournament.Create(
            cmd.Name, sport, format, cmd.ActorUserId, tokenHash,
            customRules, cmd.IndoorSwitchEverySets, cmd.BeachAutoSwitchSides,
            cmd.CustomTimeoutsPerSet, cmd.CustomTimeoutDurationSeconds,
            cmd.GroupCount, cmd.AdvancePerGroup);

        await repo.AddAsync(t, ct);
        await repo.SaveChangesAsync(ct);

        return new CreateTournamentResponseDto(mapper.ToDto(t, cmd.ActorUserId, rawToken), rawToken);
    }
}
