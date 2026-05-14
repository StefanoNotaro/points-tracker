using PointsTracker.Domain.Entities;

namespace PointsTracker.Application.Services;

public interface ICounterAuthorizationService
{
    CounterAccess GetAccess(Counter counter, Guid? userId, string? sessionToken, string? shareToken);
    CounterAccess GetLiveAccess(Counter counter, Guid? userId, string? sessionToken, string? shareToken, bool isSuperAdmin = false);
}

public record CounterAccess(bool IsOwner, bool CanEdit, bool CanRead);
