using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Infrastructure.Services;

public class TournamentAuthorizationService(IShareTokenService tokens) : ITournamentAuthorizationService
{
    public TournamentAccess GetAccess(Tournament tournament, Guid? userId, string? sessionToken)
    {
        // Owner check
        if (userId is not null && tournament.OwnerUserId == userId)
            return new TournamentAccess(true, true, true);

        // Anonymous owner via session token
        if (!string.IsNullOrEmpty(sessionToken) && tournament.SessionTokenHash is not null)
        {
            var hash = tokens.HashToken(sessionToken);
            if (string.Equals(hash, tournament.SessionTokenHash, StringComparison.Ordinal))
                return new TournamentAccess(true, true, true);
        }

        // Everyone else can read (tournaments are public by default)
        return new TournamentAccess(false, false, true);
    }
}
