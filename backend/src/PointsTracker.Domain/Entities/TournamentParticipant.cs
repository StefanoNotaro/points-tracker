namespace PointsTracker.Domain.Entities;

public class TournamentParticipant
{
    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public string TeamName { get; private set; } = string.Empty;
    public int? Seed { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime RegisteredAt { get; private set; } = DateTime.UtcNow;

    private TournamentParticipant() { }

    internal static TournamentParticipant Create(Guid tournamentId, string teamName, int? seed, Guid? userId) =>
        new()
        {
            TournamentId = tournamentId,
            TeamName     = teamName,
            Seed         = seed,
            UserId       = userId,
        };

    internal void Rename(string newName) => TeamName = newName;
    internal void SetSeed(int? seed) => Seed = seed;
}
