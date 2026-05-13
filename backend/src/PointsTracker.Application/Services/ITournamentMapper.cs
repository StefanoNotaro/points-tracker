using PointsTracker.Application.Tournaments.DTOs;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Application.Services;

public interface ITournamentMapper
{
    TournamentDto ToDto(Tournament tournament, Guid? actorUserId, string? sessionToken);
}

public interface ITournamentAuthorizationService
{
    TournamentAccess GetAccess(Tournament tournament, Guid? userId, string? sessionToken);
}

public record TournamentAccess(bool IsOwner, bool CanEdit, bool CanRead);
