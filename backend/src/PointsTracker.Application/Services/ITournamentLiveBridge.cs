namespace PointsTracker.Application.Services;

/// <summary>
/// Hook called by counter endpoints after every counter-modifying action.
/// If the counter is linked to a tournament match the bridge reconciles
/// the bracket (records the winner when the counter finishes) and pushes
/// a fresh TournamentDto over the TournamentHub so brackets and standings
/// stay live without forcing the UI to re-fetch.
///
/// No-op when the counter has no linked match — cheap path.
/// </summary>
public interface ITournamentLiveBridge
{
    Task OnCounterChangedAsync(Guid counterId, CancellationToken ct = default);
}
