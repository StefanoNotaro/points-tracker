using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record SwitchSidesCommand(
    Guid CounterId,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken,
    string? ScorerToken = null
) : IRequest<CounterDto>;

public class SwitchSidesValidator : AbstractValidator<SwitchSidesCommand>
{
    public SwitchSidesValidator()
    {
        RuleFor(x => x.CounterId).NotEmpty();
    }
}

public class SwitchSidesHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<SwitchSidesCommand, CounterDto>
{
    public async Task<CounterDto> Handle(SwitchSidesCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        var canScore = !access.CanEdit && await authService.HasScorerAccessAsync(counter, cmd.ScorerToken, ct);
        if (!access.CanEdit && !canScore) throw new ForbiddenException();

        counter.SwitchSidesManually();

        await counterRepo.SaveChangesAsync(ct);
        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken, canScore);
    }
}
