using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Enums;

namespace PointsTracker.Infrastructure.Services;

public class ShareTokenService(IConfiguration config) : IShareTokenService
{
    private readonly string _secret = config["ShareToken:Secret"]
        ?? throw new InvalidOperationException("ShareToken:Secret is not configured.");

    public string GenerateSessionToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifyToken(string token, string hash) =>
        HashToken(token) == hash;

    public string GenerateShareToken(Guid counterId, ShareScope scope)
    {
        var payload = $"{counterId}:{scope}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var secretBytes = Encoding.UTF8.GetBytes(_secret);

        var hmac = HMACSHA256.HashData(secretBytes, payloadBytes);
        var signature = Convert.ToBase64String(hmac).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        return $"{Convert.ToBase64String(payloadBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=')}.{signature}";
    }
}
