using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record UpdateTeamNameCommand(
    Guid CounterId,
    string Team,
    string Name,
    Guid? ActorUserId,
    string? SessionToken,
    string? ShareToken
) : IRequest<CounterDto>;

public class UpdateTeamNameValidator : AbstractValidator<UpdateTeamNameCommand>
{
    public UpdateTeamNameValidator()
    {
        RuleFor(x => x.CounterId).NotEmpty();
        RuleFor(x => x.Team).Must(v => v is "A" or "B").WithMessage("Team must be A or B.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateTeamNameHandler(
    ICounterRepository counterRepo,
    ICounterAuthorizationService authService,
    ICounterMapper mapper
) : IRequestHandler<UpdateTeamNameCommand, CounterDto>
{
    public async Task<CounterDto> Handle(UpdateTeamNameCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
        if (!access.CanEdit) throw new ForbiddenException();

        var team = cmd.Team == "A" ? Team.A : Team.B;
        counter.UpdateTeamName(team, cmd.Name);
        await counterRepo.SaveChangesAsync(ct);

        return mapper.ToDto(counter, cmd.ActorUserId, cmd.SessionToken, cmd.ShareToken);
    }
}
