using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record CallTimeoutCommand(
    Guid CounterId,
    string Team,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken,
    string? ScorerToken = null
) : IRequest<CounterDto>;

public class CallTimeoutValidator : AbstractValidator<CallTimeoutCommand>
{
    public CallTimeoutValidator()
    {
        RuleFor(x => x.Team)
            .NotEmpty()
            .Must(v => Enum.TryParse<Team>(v, true, out _))
            .WithMessage("Team must be 'A' or 'B'.");
    }
}

public class CallTimeoutHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<CallTimeoutCommand, CounterDto>
{
    public async Task<CounterDto> Handle(CallTimeoutCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        var canScore = !access.CanEdit && await authService.HasScorerAccessAsync(counter, cmd.ScorerToken, ct);
        if (!access.CanEdit && !canScore) throw new ForbiddenException();

        var team = Enum.Parse<Team>(cmd.Team, true);
        counter.CallTimeout(team, cmd.ActorUserId);
        await counterRepo.SaveChangesAsync(ct);
        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken, canScore);
    }
}
