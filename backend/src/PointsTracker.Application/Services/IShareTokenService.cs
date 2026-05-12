using PointsTracker.Domain.Enums;

namespace PointsTracker.Application.Services;

public interface IShareTokenService
{
    string GenerateSessionToken();
    string HashToken(string token);
    bool VerifyToken(string token, string hash);
    string GenerateShareToken(Guid counterId, ShareScope scope);
}
