using System.Security.Cryptography;
using System.Text;
using PointsTracker.Application.Services;

namespace PointsTracker.Infrastructure.Services;

public class ShareTokenService : IShareTokenService
{
    private const int SessionTokenBytes = 32;
    private const int ShareTokenBytes = 12;

    public string GenerateSessionToken() =>
        ToBase64Url(RandomNumberGenerator.GetBytes(SessionTokenBytes));

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifyToken(string token, string hash) =>
        HashToken(token) == hash;

    public string GenerateShareToken() =>
        ToBase64Url(RandomNumberGenerator.GetBytes(ShareTokenBytes));

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
}
