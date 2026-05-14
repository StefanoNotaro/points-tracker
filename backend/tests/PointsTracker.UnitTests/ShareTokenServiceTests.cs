using PointsTracker.Infrastructure.Services;

namespace PointsTracker.UnitTests;

public class ShareTokenServiceTests
{
    [Fact]
    public void GenerateShareToken_ReturnsShortUrlSafeToken()
    {
        var sut = new ShareTokenService();

        var token = sut.GenerateShareToken();

        Assert.Equal(16, token.Length);
        Assert.Matches("^[A-Za-z0-9_-]{16}$", token);
    }

    [Fact]
    public void GenerateShareToken_GeneratesDistinctTokensAcrossSample()
    {
        var sut = new ShareTokenService();

        var tokens = Enumerable.Range(0, 128)
            .Select(_ => sut.GenerateShareToken())
            .ToList();

        Assert.Equal(tokens.Count, tokens.Distinct(StringComparer.Ordinal).Count());
    }
}

