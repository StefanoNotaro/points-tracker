using PointsTracker.Application.Counters.Commands;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Entities;
using PointsTracker.Domain.Enums;
using PointsTracker.Domain.Exceptions;
using PointsTracker.Domain.Interfaces;

namespace PointsTracker.UnitTests;

public class CreateShareTokenHandlerTests
{
    [Fact]
    public async Task Handle_WhenGeneratedTokenAlreadyExists_RetriesUntilUniqueTokenIsFound()
    {
        var ownerId = Guid.NewGuid();
        var counter = Counter.Create(SportType.Volleyball, "Team A", "Team B", ownerId, sessionTokenHash: null);
        var counterRepo = new FakeCounterRepository(counter);
        var shareTokenRepo = new FakeShareTokenRepository(existingTokens: ["duplicate-token"]);
        var tokenService = new StubShareTokenService("duplicate-token", "short-token-1234");
        var authService = new StubCounterAuthorizationService(new CounterAccess(IsOwner: true, CanEdit: true, CanRead: true));
        var sut = new CreateShareTokenHandler(counterRepo, shareTokenRepo, authService, tokenService);

        var result = await sut.Handle(
            new CreateShareTokenCommand(counter.Id, "edit", ownerId, SessionToken: null, BaseUrl: "https://app.example.com"),
            CancellationToken.None);

        Assert.Equal("short-token-1234", result.Token);
        Assert.Equal("https://app.example.com/counter/join/short-token-1234", result.ShareUrl);
        Assert.Equal("edit", result.Scope);
        Assert.Equal(2, tokenService.GenerateShareTokenCalls);
        Assert.Single(shareTokenRepo.AddedTokens);
        Assert.Equal("short-token-1234", shareTokenRepo.AddedTokens[0].Token);
        Assert.Equal(ShareScope.Edit, shareTokenRepo.AddedTokens[0].Scope);
    }

    [Fact]
    public async Task Handle_WhenActorIsNotOwner_ThrowsForbiddenException()
    {
        var counter = Counter.Create(SportType.Volleyball, "Team A", "Team B", Guid.NewGuid(), sessionTokenHash: null);
        var sut = new CreateShareTokenHandler(
            new FakeCounterRepository(counter),
            new FakeShareTokenRepository(),
            new StubCounterAuthorizationService(new CounterAccess(IsOwner: false, CanEdit: false, CanRead: true)),
            new StubShareTokenService("unused-token-000"));

        var act = () => sut.Handle(
            new CreateShareTokenCommand(counter.Id, "read", Guid.NewGuid(), SessionToken: null, BaseUrl: "https://app.example.com"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    private sealed class FakeCounterRepository(Counter counter) : ICounterRepository
    {
        public Task<Counter?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(id == counter.Id ? counter : null);

        public Task<Counter> AddAsync(Counter newCounter, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Counter>> ListByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Counter>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Counter>> ListByTournamentAsync(Guid tournamentId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeShareTokenRepository(IEnumerable<string>? existingTokens = null) : IShareTokenRepository
    {
        private readonly HashSet<string> _existingTokens = new(existingTokens ?? [], StringComparer.Ordinal);

        public List<ShareToken> AddedTokens { get; } = [];

        public Task<ShareToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        {
            if (!_existingTokens.Contains(token))
                return Task.FromResult<ShareToken?>(null);

            var existing = ShareToken.Create(Guid.NewGuid(), token, ShareScope.Read, createdByUserId: null, TimeSpan.FromDays(7));
            return Task.FromResult<ShareToken?>(existing);
        }

        public Task AddAsync(ShareToken shareToken, CancellationToken ct = default)
        {
            AddedTokens.Add(shareToken);
            _existingTokens.Add(shareToken.Token);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class StubCounterAuthorizationService(CounterAccess access) : ICounterAuthorizationService
    {
        public CounterAccess GetAccess(Counter counter, Guid? userId, string? sessionToken, string? shareToken) => access;

        public CounterAccess GetLiveAccess(Counter counter, Guid? userId, string? sessionToken, string? shareToken, bool isSuperAdmin = false) => access;
    }

    private sealed class StubShareTokenService(params string[] generatedTokens) : IShareTokenService
    {
        private readonly Queue<string> _generatedTokens = new(generatedTokens);

        public int GenerateShareTokenCalls { get; private set; }

        public string GenerateSessionToken() => "session-token";

        public string HashToken(string token) => token;

        public bool VerifyToken(string token, string hash) =>
            string.Equals(token, hash, StringComparison.Ordinal);

        public string GenerateShareToken()
        {
            GenerateShareTokenCalls++;
            if (_generatedTokens.Count == 0)
                throw new InvalidOperationException("No generated share tokens were configured for this test.");

            return _generatedTokens.Dequeue();
        }
    }
}


