using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record UpdateScoreCommand(
    Guid CounterId,
    string Team,
    bool Increment,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken
) : IRequest<CounterDto>;

public class UpdateScoreValidator : AbstractValidator<UpdateScoreCommand>
{
    public UpdateScoreValidator()
    {
        RuleFor(x => x.CounterId).NotEmpty();
        RuleFor(x => x.Team).Must(v => v is "A" or "B").WithMessage("Team must be A or B.");
    }
}

public class UpdateScoreHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<UpdateScoreCommand, CounterDto>
{
    public async Task<CounterDto> Handle(UpdateScoreCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        if (!access.CanEdit) throw new ForbiddenException();

        var team = cmd.Team == "A" ? Team.A : Team.B;

        if (cmd.Increment) counter.IncrementScore(team, cmd.ActorUserId);
        else counter.DecrementScore(team, cmd.ActorUserId);

        await counterRepo.SaveChangesAsync(ct);
        return mapper.ToDto(counter, cmd.ActorUserId, cmd.ShareToken);
    }
}
