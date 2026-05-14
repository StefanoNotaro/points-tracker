using FluentValidation;
using MediatR;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Application.Admin.Commands;

// ---- Preview (dry-run) ---------------------------------------------------

public record GetCleanupPreviewQuery() : IRequest<CleanupPreview>;

public class GetCleanupPreviewHandler(ICleanupService cleanup) : IRequestHandler<GetCleanupPreviewQuery, CleanupPreview>
{
    public Task<CleanupPreview> Handle(GetCleanupPreviewQuery request, CancellationToken ct) =>
        cleanup.PreviewAsync(ct);
}

// ---- Policy run ----------------------------------------------------------

public record RunCleanupPolicyCommand(Guid ActorUserId, bool Confirm, string? Reason)
    : IRequest<CleanupRunResult>;

public class RunCleanupPolicyValidator : AbstractValidator<RunCleanupPolicyCommand>
{
    public RunCleanupPolicyValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Confirm).Equal(true).WithMessage("Policy run must be confirmed.");
        RuleFor(x => x.Reason).MaximumLength(1024);
    }
}

public class RunCleanupPolicyHandler(
    ICleanupService cleanup,
    ICleanupAuditLogRepository auditRepo) : IRequestHandler<RunCleanupPolicyCommand, CleanupRunResult>
{
    public async Task<CleanupRunResult> Handle(RunCleanupPolicyCommand cmd, CancellationToken ct)
    {
        var result = await cleanup.RunPolicyAsync(ct);
        var total = result.CountersSoftDeleted + result.TournamentsSoftDeleted
                    + result.CountersHardPurged + result.TournamentsHardPurged
                    + result.ShareTokensPurged;

        if (total > 0)
        {
            await auditRepo.AddAsync(
                CleanupAuditLog.Record(
                    CleanupAction.RunPolicy,
                    actor: $"admin:{cmd.ActorUserId}",
                    targetCount: total,
                    targetIds: null,
                    reason: cmd.Reason),
                ct);
            await auditRepo.SaveChangesAsync(ct);
        }
        return result;
    }
}

// ---- Ad-hoc soft-delete by id -------------------------------------------

public record SoftDeleteCountersCommand(
    IReadOnlyCollection<Guid> Ids,
    Guid ActorUserId,
    bool Confirm,
    string? Reason) : IRequest<int>;

public class SoftDeleteCountersValidator : AbstractValidator<SoftDeleteCountersCommand>
{
    public SoftDeleteCountersValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().WithMessage("At least one id is required.");
        RuleFor(x => x.Ids.Count).LessThanOrEqualTo(500)
            .WithMessage("Batch size limited to 500 ids per call.");
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Confirm).Equal(true).WithMessage("Soft-delete must be confirmed.");
        RuleFor(x => x.Reason).MaximumLength(1024);
    }
}

public class SoftDeleteCountersHandler(
    ICleanupService cleanup,
    ICleanupAuditLogRepository auditRepo) : IRequestHandler<SoftDeleteCountersCommand, int>
{
    public async Task<int> Handle(SoftDeleteCountersCommand cmd, CancellationToken ct)
    {
        var affected = await cleanup.SoftDeleteCountersAsync(cmd.Ids, ct);
        if (affected > 0)
        {
            await auditRepo.AddAsync(
                CleanupAuditLog.Record(CleanupAction.SoftDeleteCounters,
                    $"admin:{cmd.ActorUserId}", affected, cmd.Ids, cmd.Reason), ct);
            await auditRepo.SaveChangesAsync(ct);
        }
        return affected;
    }
}

public record SoftDeleteTournamentsCommand(
    IReadOnlyCollection<Guid> Ids,
    Guid ActorUserId,
    bool Confirm,
    string? Reason) : IRequest<int>;

public class SoftDeleteTournamentsValidator : AbstractValidator<SoftDeleteTournamentsCommand>
{
    public SoftDeleteTournamentsValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
        RuleFor(x => x.Ids.Count).LessThanOrEqualTo(500);
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Confirm).Equal(true).WithMessage("Soft-delete must be confirmed.");
        RuleFor(x => x.Reason).MaximumLength(1024);
    }
}

public class SoftDeleteTournamentsHandler(
    ICleanupService cleanup,
    ICleanupAuditLogRepository auditRepo) : IRequestHandler<SoftDeleteTournamentsCommand, int>
{
    public async Task<int> Handle(SoftDeleteTournamentsCommand cmd, CancellationToken ct)
    {
        var affected = await cleanup.SoftDeleteTournamentsAsync(cmd.Ids, ct);
        if (affected > 0)
        {
            await auditRepo.AddAsync(
                CleanupAuditLog.Record(CleanupAction.SoftDeleteTournaments,
                    $"admin:{cmd.ActorUserId}", affected, cmd.Ids, cmd.Reason), ct);
            await auditRepo.SaveChangesAsync(ct);
        }
        return affected;
    }
}

// ---- Ad-hoc hard-purge by id (super_admin) ------------------------------

public record HardPurgeCountersCommand(
    IReadOnlyCollection<Guid> Ids,
    Guid ActorUserId,
    bool Confirm,
    string Reason) : IRequest<int>;

public class HardPurgeCountersValidator : AbstractValidator<HardPurgeCountersCommand>
{
    public HardPurgeCountersValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
        RuleFor(x => x.Ids.Count).LessThanOrEqualTo(500);
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Confirm).Equal(true).WithMessage("Hard-purge must be confirmed.");
        RuleFor(x => x.Reason).NotEmpty()
            .WithMessage("Hard-purge requires a reason (this action is irreversible).")
            .MaximumLength(1024);
    }
}

public class HardPurgeCountersHandler(
    ICleanupService cleanup,
    ICleanupAuditLogRepository auditRepo) : IRequestHandler<HardPurgeCountersCommand, int>
{
    public async Task<int> Handle(HardPurgeCountersCommand cmd, CancellationToken ct)
    {
        var affected = await cleanup.HardPurgeCountersAsync(cmd.Ids, ct);
        if (affected > 0)
        {
            await auditRepo.AddAsync(
                CleanupAuditLog.Record(CleanupAction.HardPurgeCounters,
                    $"admin:{cmd.ActorUserId}", affected, cmd.Ids, cmd.Reason), ct);
            await auditRepo.SaveChangesAsync(ct);
        }
        return affected;
    }
}

public record HardPurgeTournamentsCommand(
    IReadOnlyCollection<Guid> Ids,
    Guid ActorUserId,
    bool Confirm,
    string Reason) : IRequest<int>;

public class HardPurgeTournamentsValidator : AbstractValidator<HardPurgeTournamentsCommand>
{
    public HardPurgeTournamentsValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
        RuleFor(x => x.Ids.Count).LessThanOrEqualTo(500);
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Confirm).Equal(true).WithMessage("Hard-purge must be confirmed.");
        RuleFor(x => x.Reason).NotEmpty()
            .WithMessage("Hard-purge requires a reason (this action is irreversible).")
            .MaximumLength(1024);
    }
}

public class HardPurgeTournamentsHandler(
    ICleanupService cleanup,
    ICleanupAuditLogRepository auditRepo) : IRequestHandler<HardPurgeTournamentsCommand, int>
{
    public async Task<int> Handle(HardPurgeTournamentsCommand cmd, CancellationToken ct)
    {
        var affected = await cleanup.HardPurgeTournamentsAsync(cmd.Ids, ct);
        if (affected > 0)
        {
            await auditRepo.AddAsync(
                CleanupAuditLog.Record(CleanupAction.HardPurgeTournaments,
                    $"admin:{cmd.ActorUserId}", affected, cmd.Ids, cmd.Reason), ct);
            await auditRepo.SaveChangesAsync(ct);
        }
        return affected;
    }
}

// ---- Expired share tokens -----------------------------------------------

public record PurgeExpiredShareTokensCommand(Guid ActorUserId, bool Confirm) : IRequest<int>;

public class PurgeExpiredShareTokensValidator : AbstractValidator<PurgeExpiredShareTokensCommand>
{
    public PurgeExpiredShareTokensValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Confirm).Equal(true).WithMessage("Sweep must be confirmed.");
    }
}

public class PurgeExpiredShareTokensHandler(
    ICleanupService cleanup,
    ICleanupAuditLogRepository auditRepo) : IRequestHandler<PurgeExpiredShareTokensCommand, int>
{
    public async Task<int> Handle(PurgeExpiredShareTokensCommand cmd, CancellationToken ct)
    {
        var affected = await cleanup.PurgeExpiredShareTokensAsync(ct);
        if (affected > 0)
        {
            await auditRepo.AddAsync(
                CleanupAuditLog.Record(CleanupAction.PurgeExpiredShareTokens,
                    $"admin:{cmd.ActorUserId}", affected), ct);
            await auditRepo.SaveChangesAsync(ct);
        }
        return affected;
    }
}

// ---- Audit log read -----------------------------------------------------

public record GetCleanupAuditLogQuery(int Take = 100) : IRequest<IReadOnlyList<CleanupAuditLog>>;

public class GetCleanupAuditLogHandler(ICleanupAuditLogRepository repo)
    : IRequestHandler<GetCleanupAuditLogQuery, IReadOnlyList<CleanupAuditLog>>
{
    public Task<IReadOnlyList<CleanupAuditLog>> Handle(GetCleanupAuditLogQuery query, CancellationToken ct) =>
        repo.GetRecentAsync(Math.Clamp(query.Take, 1, 500), ct);
}
