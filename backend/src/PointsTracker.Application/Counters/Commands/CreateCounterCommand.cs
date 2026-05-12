using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record CreateCounterCommand(
    string SportType,
    string TeamAName,
    string TeamBName,
    Guid? ActorUserId
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

        var counter = Counter.Create(sport, cmd.TeamAName, cmd.TeamBName, cmd.ActorUserId, tokenHash);
        await counterRepo.AddAsync(counter, ct);
        await counterRepo.SaveChangesAsync(ct);

        return new CreateCounterResponseDto(mapper.ToDto(counter, cmd.ActorUserId, null), rawToken);
    }
}
