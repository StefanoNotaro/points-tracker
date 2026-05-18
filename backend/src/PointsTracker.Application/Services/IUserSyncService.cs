using System.Security.Claims;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Application.Services;

public interface IUserSyncService
{
    Task<User> SyncAsync(ClaimsPrincipal principal, CancellationToken ct = default);
}
