using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record ResolveSideSwitchCommand(
    Guid CounterId,
    bool Confirm,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken,
    string? ScorerToken = null
) : IRequest<CounterDto>;

public class ResolveSideSwitchValidator : AbstractValidator<ResolveSideSwitchCommand>
{
    public ResolveSideSwitchValidator()
    {
        RuleFor(x => x.CounterId).NotEmpty();
    }
}

public class ResolveSideSwitchHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<ResolveSideSwitchCommand, CounterDto>
{
    public async Task<CounterDto> Handle(ResolveSideSwitchCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        var canScore = !access.CanEdit && await authService.HasScorerAccessAsync(counter, cmd.ScorerToken, ct);
        if (!access.CanEdit && !canScore) throw new ForbiddenException();

        if (cmd.Confirm) counter.ConfirmSideSwitch();
        else counter.DismissSideSwitch();

        await counterRepo.SaveChangesAsync(ct);
        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken, canScore);
    }
}
