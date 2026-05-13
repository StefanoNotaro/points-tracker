using Microsoft.AspNetCore.SignalR;

namespace PointsTracker.Infrastructure.Hubs;

/// <summary>
/// Resolves the SignalR user identifier from the internal "pts_id" claim
/// rather than the default <c>NameIdentifier</c> (which is the OIDC <c>sub</c>
/// — an external Authentik id). The internal Guid is the one used as
/// <c>Counter.OwnerUserId</c>, so this lines up the per-user broadcast group
/// (<c>user-{OwnerUserId}</c>) with the connection's UserIdentifier and
/// avoids the dashboard subscribing to the wrong group entirely.
/// </summary>
public class PtsIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("pts_id")?.Value;
}
