namespace PointsTracker.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required string ExternalId { get; init; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; private set; }

    private User() { }

    public static User Create(string externalId, string email, string displayName) =>
        new() { ExternalId = externalId, Email = email, DisplayName = displayName };

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
