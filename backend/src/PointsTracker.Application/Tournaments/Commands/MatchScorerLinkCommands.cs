using FluentValidation;
using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Tournaments.Commands;

// ── Issue ─────────────────────────────────────────────────────────────────────

public record IssueMatchScorerLinkCommand(
    Guid TournamentId,
    Guid MatchId,
    Guid ActorUserId,
    string? ActorSessionToken,
    string? Label = null,
    Guid? GrantToUserId = null
) : IRequest<IssuedMatchScorerLinkDto>;

public class IssueMatchScorerLinkValidator : AbstractValidator<IssueMatchScorerLinkCommand>
{
    public IssueMatchScorerLinkValidator()
    {
        RuleFor(x => x.TournamentId).NotEmpty();
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.Label).MaximumLength(100).When(x => x.Label is not null);
    }
}

public class IssueMatchScorerLinkHandler(
    ITournamentRepository tournaments,
    IMatchScorerLinkRepository scorerLinks,
    ITournamentAuthorizationService auth,
    IShareTokenService tokenService
) : IRequestHandler<IssueMatchScorerLinkCommand, IssuedMatchScorerLinkDto>
{
    public async Task<IssuedMatchScorerLinkDto> Handle(IssueMatchScorerLinkCommand cmd, CancellationToken ct)
    {
        var t = await tournaments.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);

        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit)
            throw new ForbiddenException("Only the tournament organiser can issue scorer links.");

        // Verify the match exists within this tournament.
        t.GetMatch(cmd.MatchId);

        var rawToken = tokenService.GenerateShareToken();
        var tokenHash = tokenService.HashToken(rawToken);

        var link = MatchScorerLink.Create(
            cmd.TournamentId,
            cmd.MatchId,
            tokenHash,
            createdByUserId: cmd.ActorUserId,
            grantedToUserId: cmd.GrantToUserId,
            label: cmd.Label);

        await scorerLinks.AddAsync(link, ct);
        await scorerLinks.SaveChangesAsync(ct);

        return new IssuedMatchScorerLinkDto(
            link.Id,
            link.TournamentId,
            link.MatchId,
            link.Label,
            link.GrantedToUserId,
            rawToken,
            link.CreatedAt);
    }
}

// ── Revoke ────────────────────────────────────────────────────────────────────

public record RevokeMatchScorerLinkCommand(
    Guid TournamentId,
    Guid LinkId,
    Guid ActorUserId,
    string? ActorSessionToken
) : IRequest;

public class RevokeMatchScorerLinkHandler(
    ITournamentRepository tournaments,
    IMatchScorerLinkRepository scorerLinks,
    ITournamentAuthorizationService auth
) : IRequestHandler<RevokeMatchScorerLinkCommand>
{
    public async Task Handle(RevokeMatchScorerLinkCommand cmd, CancellationToken ct)
    {
        var t = await tournaments.GetByIdAsync(cmd.TournamentId, ct)
            ?? throw new NotFoundException("Tournament", cmd.TournamentId);

        var access = auth.GetAccess(t, cmd.ActorUserId, cmd.ActorSessionToken);
        if (!access.CanEdit)
            throw new ForbiddenException("Only the tournament organiser can revoke scorer links.");

        var link = await scorerLinks.GetByIdAsync(cmd.LinkId, ct)
            ?? throw new NotFoundException("MatchScorerLink", cmd.LinkId);

        if (link.TournamentId != cmd.TournamentId)
            throw new ForbiddenException("Scorer link does not belong to this tournament.");

        link.Revoke();
        await scorerLinks.SaveChangesAsync(ct);
    }
}
