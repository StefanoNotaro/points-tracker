using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record CustomRulesInput(int PointsPerSet, int LastSetPoints, int SetsToWin, int TotalSets, bool WinByTwo);

public record CreateCounterCommand(
    string SportType,
    string TeamAName,
    string TeamBName,
    Guid? ActorUserId,
    CustomRulesInput? CustomRules,
    int? IndoorSwitchEverySets = null,
    bool BeachAutoSwitchSides = true
) : IRequest<CreateCounterResponseDto>;

public class CreateCounterValidator : AbstractValidator<CreateCounterCommand>
{
    public CreateCounterValidator()
    {
        RuleFor(x => x.SportType)
            .NotEmpty()
            .Must(v => Enum.TryParse<SportType>(v, true, out _))
            .WithMessage("Invalid sport type.");

        RuleFor(x => x.TeamAName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TeamBName).NotEmpty().MaximumLength(100);

        When(x => string.Equals(x.SportType, "custom", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CustomRules)
                .NotNull().WithMessage("Custom sport requires rules.");
        });

        RuleFor(x => x.IndoorSwitchEverySets)
            .Must(v => v is null or 1 or 2)
            .WithMessage("Indoor side-switch interval must be 1 or 2.");

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

public class CreateCounterHandler(
    ICounterRepository counterRepo,
    IShareTokenService tokenService,
    ICounterMapper mapper
) : IRequestHandler<CreateCounterCommand, CreateCounterResponseDto>
{
    public async Task<CreateCounterResponseDto> Handle(CreateCounterCommand cmd, CancellationToken ct)
    {
        var sport = Enum.Parse<SportType>(cmd.SportType, true);
        string? rawToken = null;
        string? tokenHash = null;

        if (cmd.ActorUserId is null)
        {
            rawToken = tokenService.GenerateSessionToken();
            tokenHash = tokenService.HashToken(rawToken);
        }

        SportRules? customRules = cmd.CustomRules is null
            ? null
            : new SportRules(
                cmd.CustomRules.PointsPerSet,
                cmd.CustomRules.LastSetPoints,
                cmd.CustomRules.SetsToWin,
                cmd.CustomRules.TotalSets,
                cmd.CustomRules.WinByTwo);

        var counter = Counter.Create(sport, cmd.TeamAName, cmd.TeamBName, cmd.ActorUserId, tokenHash, customRules, cmd.IndoorSwitchEverySets, cmd.BeachAutoSwitchSides);
        await counterRepo.AddAsync(counter, ct);
        await counterRepo.SaveChangesAsync(ct);

        // Pass the just-generated session token so the returned DTO reflects owner access for anonymous creators.
        return new CreateCounterResponseDto(mapper.ToDto(counter, cmd.ActorUserId, rawToken, null), rawToken);
    }
}
