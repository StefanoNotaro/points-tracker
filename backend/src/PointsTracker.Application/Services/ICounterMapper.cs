using PointsTracker.Application.Counters.DTOs;
using PointsTracker.Domain.Entities;

namespace PointsTracker.Application.Services;

public interface ICounterMapper
{
    CounterDto ToDto(Counter counter, Guid? actorUserId, string? shareToken);
}
