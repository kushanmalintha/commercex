using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.Logout;
using CommerceX.Auth.Application.UseCases.Logout;
using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.UnitTests.UseCases.Logout;

public sealed class LogoutUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRevokeRefreshToken_WhenTokenIsValid()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026,
            9,
            26,
            10,
            0,
            0,
            TimeSpan.Zero);

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hashed:test-refresh-token",
            now.AddDays(30),
            now);

        var refreshTokenRepository =
            new FakeRefreshTokenRepository(refreshToken);

        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LogoutUseCase(
            refreshTokenRepository,
            tokenHasher,
            dateTimeProvider,
            unitOfWork);

        var request = new LogoutRequest(
            "test-refresh-token");

        // Act
        LogoutResponse result =
            await useCase.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);

        Assert.NotNull(
            refreshTokenRepository.UpdatedRefreshToken);

        Assert.Equal(
            now,
            refreshTokenRepository
                .UpdatedRefreshToken!
                .RevokedAt);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenTokenDoesNotExist()
    {
        // Arrange
        var refreshTokenRepository =
            new FakeRefreshTokenRepository(null);

        var tokenHasher = new FakeTokenHasher();

        var dateTimeProvider =
            new FakeDateTimeProvider(
                new DateTimeOffset(
                    2026,
                    9,
                    26,
                    10,
                    0,
                    0,
                    TimeSpan.Zero));

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LogoutUseCase(
            refreshTokenRepository,
            tokenHasher,
            dateTimeProvider,
            unitOfWork);

        var request = new LogoutRequest(
            "unknown-refresh-token");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "Invalid refresh token.",
            exception.Message);

        Assert.Null(
            refreshTokenRepository.UpdatedRefreshToken);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsExpired()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026,
            9,
            26,
            10,
            0,
            0,
            TimeSpan.Zero);

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hashed:test-refresh-token",
            now.AddSeconds(-1),
            now.AddDays(-30));

        var refreshTokenRepository =
            new FakeRefreshTokenRepository(refreshToken);

        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LogoutUseCase(
            refreshTokenRepository,
            tokenHasher,
            dateTimeProvider,
            unitOfWork);

        var request = new LogoutRequest(
            "test-refresh-token");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "Invalid refresh token.",
            exception.Message);

        Assert.Null(
            refreshTokenRepository.UpdatedRefreshToken);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsAlreadyRevoked()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026,
            9,
            26,
            10,
            0,
            0,
            TimeSpan.Zero);

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hashed:test-refresh-token",
            now.AddDays(30),
            now.AddDays(-1));

        refreshToken.Revoke(
            now.AddHours(-1));

        var refreshTokenRepository =
            new FakeRefreshTokenRepository(refreshToken);

        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LogoutUseCase(
            refreshTokenRepository,
            tokenHasher,
            dateTimeProvider,
            unitOfWork);

        var request = new LogoutRequest(
            "test-refresh-token");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "Invalid refresh token.",
            exception.Message);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        var refreshTokenRepository =
            new FakeRefreshTokenRepository(null);

        var tokenHasher = new FakeTokenHasher();

        var dateTimeProvider =
            new FakeDateTimeProvider(
                new DateTimeOffset(
                    2026,
                    9,
                    26,
                    10,
                    0,
                    0,
                    TimeSpan.Zero));

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LogoutUseCase(
            refreshTokenRepository,
            tokenHasher,
            dateTimeProvider,
            unitOfWork);

        var request = new LogoutRequest(
            string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(request));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHashRefreshTokenBeforeRepositoryLookup()
    {
        // Arrange
        var now = new DateTimeOffset(
            2026,
            9,
            26,
            10,
            0,
            0,
            TimeSpan.Zero);

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "hashed:test-refresh-token",
            now.AddDays(30),
            now);

        var refreshTokenRepository =
            new FakeRefreshTokenRepository(refreshToken);

        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LogoutUseCase(
            refreshTokenRepository,
            tokenHasher,
            dateTimeProvider,
            unitOfWork);

        var request = new LogoutRequest(
            "test-refresh-token");

        // Act
        await useCase.ExecuteAsync(request);

        // Assert
        Assert.Equal(
            "hashed:test-refresh-token",
            refreshTokenRepository.LastRequestedTokenHash);
    }

    private sealed class FakeRefreshTokenRepository
        : IRefreshTokenRepository
    {
        private readonly RefreshToken? _refreshToken;

        public string? LastRequestedTokenHash { get; private set; }

        public RefreshToken? UpdatedRefreshToken { get; private set; }

        public FakeRefreshTokenRepository(
            RefreshToken? refreshToken)
        {
            _refreshToken = refreshToken;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            LastRequestedTokenHash = tokenHash;

            return Task.FromResult(_refreshToken);
        }

        public Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            UpdatedRefreshToken = refreshToken;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTokenHasher : ITokenHasher
    {
        public string Hash(string token)
        {
            return $"hashed:{token}";
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; }

        public FakeDateTimeProvider(
            DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }
    }

    private sealed class FakeAuthUnitOfWork : IAuthUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.CompletedTask;
        }
    }
}