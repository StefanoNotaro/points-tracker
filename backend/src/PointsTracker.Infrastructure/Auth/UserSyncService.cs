using System.Security.Claims;
using Microsoft.Extensions.Logging;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.Infrastructure.Auth;

public class UserSyncService(IUserRepository userRepo, ILogger<UserSyncService> logger)
{
    public async Task<User> SyncAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var externalId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("No subject claim in token.");

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? throw new InvalidOperationException("No email claim in token.");

        var displayName = principal.FindFirstValue("name") ?? email;

        var user = await userRepo.GetByExternalIdAsync(externalId, ct);
        if (user is null)
        {
            user = User.Create(externalId, email, displayName);
            await userRepo.AddAsync(user, ct);
            await userRepo.SaveChangesAsync(ct);
            logger.LogInformation("Created new user {ExternalId}", externalId);
        }
        else
        {
            user.Email = email;
            user.DisplayName = displayName;
            user.Touch();
            await userRepo.SaveChangesAsync(ct);
        }

        return user;
    }
}
