using FluentValidation;
using MediatR;
using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Counters.Commands;

public record CreateShareTokenCommand(
    Guid CounterId,
    string Scope,
    Guid? ActorUserId,
    string? SessionToken,
    string BaseUrl
) : IRequest<ShareTokenDto>;

public class CreateShareTokenValidator : AbstractValidator<CreateShareTokenCommand>
{
    public CreateShareTokenValidator()
    {
        RuleFor(x => x.CounterId).NotEmpty();
        RuleFor(x => x.Scope)
            .Must(v => v is "read" or "edit")
            .WithMessage("Scope must be 'read' or 'edit'.");
    }
}

public class CreateShareTokenHandler(
    ICounterRepository counterRepo,
    IShareTokenRepository shareTokenRepo,
    ICounterAuthorizationService authService,
    IShareTokenService tokenService
) : IRequestHandler<CreateShareTokenCommand, ShareTokenDto>
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    public async Task<ShareTokenDto> Handle(CreateShareTokenCommand cmd, CancellationToken ct)
    {
        var counter = await counterRepo.GetByIdAsync(cmd.CounterId, ct)
            ?? throw new NotFoundException(nameof(Counter), cmd.CounterId);

        var access = authService.GetAccess(counter, cmd.ActorUserId, cmd.SessionToken, null);
        if (!access.IsOwner) throw new ForbiddenException("Only the owner can create share links.");

        var scope = cmd.Scope == "edit" ? ShareScope.Edit : ShareScope.Read;
        var rawToken = tokenService.GenerateShareToken(cmd.CounterId, scope);
        var shareToken = ShareToken.Create(cmd.CounterId, rawToken, scope, cmd.ActorUserId, DefaultExpiry);

        await shareTokenRepo.AddAsync(shareToken, ct);
        await shareTokenRepo.SaveChangesAsync(ct);

        return new ShareTokenDto(
            rawToken,
            $"{cmd.BaseUrl}/counter/join/{rawToken}",
            cmd.Scope,
            shareToken.ExpiresAt
        );
    }
}
