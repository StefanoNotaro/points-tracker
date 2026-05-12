using PointsTracker.Domain.Enums;

namespace PointsTracker.Domain.Entities;

public class ShareToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CounterId { get; private set; }
    public required string Token { get; init; }
    public ShareScope Scope { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private ShareToken() { }

    public static ShareToken Create(Guid counterId, string token, ShareScope scope,
        Guid? createdByUserId, TimeSpan expiry) =>
        new()
        {
            CounterId = counterId,
            Token = token,
            Scope = scope,
            CreatedByUserId = createdByUserId,
            ExpiresAt = DateTime.UtcNow.Add(expiry)
        };

    public bool IsValid => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
    public bool CanEdit => IsValid && Scope == ShareScope.Edit;

    public void Revoke() => RevokedAt = DateTime.UtcNow;
}
