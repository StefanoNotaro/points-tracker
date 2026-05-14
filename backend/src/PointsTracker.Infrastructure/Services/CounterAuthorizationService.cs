using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Infrastructure.Services;

public class CounterAuthorizationService(IShareTokenService tokenService) : ICounterAuthorizationService
{
    public CounterAccess GetAccess(Counter counter, Guid? userId, string? sessionToken, string? shareToken)
    {
        // Super-admin bypass handled at API layer via role check
        if (userId.HasValue && counter.OwnerUserId == userId)
            return new CounterAccess(IsOwner: true, CanEdit: true, CanRead: true);

        if (!string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(counter.SessionTokenHash))
        {
            if (tokenService.VerifyToken(sessionToken, counter.SessionTokenHash))
                return new CounterAccess(IsOwner: true, CanEdit: true, CanRead: true);
        }

        if (!string.IsNullOrEmpty(shareToken))
        {
            var token = counter.ShareTokens.FirstOrDefault(t => t.Token == shareToken && t.IsValid);
            if (token is not null)
                return new CounterAccess(IsOwner: false, CanEdit: token.CanEdit, CanRead: true);
        }

        // Public read for any counter (anyone can view the score)
        return new CounterAccess(IsOwner: false, CanEdit: false, CanRead: true);
    }

    public CounterAccess GetLiveAccess(
        Counter counter,
        Guid? userId,
        string? sessionToken,
        string? shareToken,
        bool isSuperAdmin = false)
    {
        if (isSuperAdmin)
            return new CounterAccess(IsOwner: false, CanEdit: true, CanRead: true);

        if (userId.HasValue && counter.OwnerUserId == userId)
            return new CounterAccess(IsOwner: true, CanEdit: true, CanRead: true);

        if (!string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(counter.SessionTokenHash))
        {
            if (tokenService.VerifyToken(sessionToken, counter.SessionTokenHash))
                return new CounterAccess(IsOwner: true, CanEdit: true, CanRead: true);
        }

        if (!string.IsNullOrEmpty(shareToken))
        {
            var token = counter.ShareTokens.FirstOrDefault(t => t.Token == shareToken && t.IsValid);
            if (token is not null)
                return new CounterAccess(IsOwner: false, CanEdit: token.CanEdit, CanRead: true);
        }

        return new CounterAccess(IsOwner: false, CanEdit: false, CanRead: false);
    }
}
