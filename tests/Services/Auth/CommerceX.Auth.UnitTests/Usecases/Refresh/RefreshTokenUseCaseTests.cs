using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.Refresh;
using CommerceX.Auth.Application.UseCases.Refresh;
using CommerceX.Auth.Domain.Entities;
using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.UnitTests.UseCases.Refresh;

public sealed class RefreshTokenUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRotateRefreshToken_WhenTokenIsValid()
    {
        // Arrange
        Guid credentialId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        DateTimeOffset now =
            new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        RefreshToken existingToken =
            new(
                Guid.NewGuid(),
                credentialId,
                "old-token-hash",
                now.AddDays(10),
                now.AddDays(-1));

        Credential credential =
            new(
                credentialId,
                userId,
                "user@example.com",
                "password-hash",
                UserRole.Customer,
                now.AddDays(-10));

        FakeRefreshTokenRepository refreshTokenRepository = new(existingToken);

        FakeCredentialRepository credentialRepository =
            new(credential);

        FakeTokenHasher tokenHasher =
            new("old-token-hash", "new-token-hash");

        FakeSecureTokenGenerator tokenGenerator =
            new("new-raw-refresh-token");

        FakeTokenService tokenService =
            new("new-access-token");

        FakeDateTimeProvider dateTimeProvider =
            new(now);

        FakeRefreshTokenLifetimeProvider lifetimeProvider =
            new(TimeSpan.FromDays(30));

        FakeAuthUnitOfWork unitOfWork =
            new();

        RefreshTokenUseCase useCase =
            new(
                refreshTokenRepository,
                credentialRepository,
                tokenGenerator,
                tokenHasher,
                tokenService,
                dateTimeProvider,
                lifetimeProvider,
                unitOfWork);

        // Act
        RefreshTokenResponse response =
            await useCase.ExecuteAsync(
                new RefreshTokenRequest("old-raw-token"));

        // Assert
        Assert.Equal(
            "new-access-token",
            response.AccessToken);

        Assert.Equal(
            "new-raw-refresh-token",
            response.RefreshToken);

        Assert.Equal(
            "Bearer",
            response.TokenType);

        Assert.Equal(
            userId,
            response.UserId);

        Assert.Equal(
            "user@example.com",
            response.Email);

        Assert.True(
            existingToken.IsRevoked());

        Assert.Single(
            refreshTokenRepository.AddedTokens);

        Assert.True(
            unitOfWork.SaveChangesCalled);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenTokenDoesNotExist()
    {
        // Arrange
        FakeRefreshTokenRepository refreshTokenRepository =
            new(null);

        RefreshTokenUseCase useCase =
            CreateUseCase(
                refreshTokenRepository);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(
                new RefreshTokenRequest("unknown-token")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsExpired()
    {
        // Arrange
        DateTimeOffset now =
            new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        RefreshToken expiredToken =
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "expired-token-hash",
                now.AddMinutes(-1),
                now.AddDays(-1));

        RefreshTokenUseCase useCase =
            CreateUseCase(
                new FakeRefreshTokenRepository(expiredToken),
                now);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(
                new RefreshTokenRequest("expired-token")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsRevoked()
    {
        // Arrange
        DateTimeOffset now =
            new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        RefreshToken revokedToken =
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "revoked-token-hash",
                now.AddDays(10),
                now.AddDays(-1));

        revokedToken.Revoke(now.AddHours(-1));

        RefreshTokenUseCase useCase =
            CreateUseCase(
                new FakeRefreshTokenRepository(revokedToken),
                now);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(
                new RefreshTokenRequest("revoked-token")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenCredentialIsInactive()
    {
        // Arrange
        DateTimeOffset now =
            new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        Guid credentialId = Guid.NewGuid();

        RefreshToken refreshToken =
            new(
                Guid.NewGuid(),
                credentialId,
                "token-hash",
                now.AddDays(10),
                now.AddDays(-1));

        Credential credential =
            new(
                credentialId,
                Guid.NewGuid(),
                "user@example.com",
                "password-hash",
                UserRole.Customer,
                now.AddDays(-10));

        credential.SetActive(false, now);

        RefreshTokenUseCase useCase =
            CreateUseCase(
                new FakeRefreshTokenRepository(refreshToken),
                now,
                credential);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(
                new RefreshTokenRequest("raw-token")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenRefreshTokenIsEmpty()
    {
        // Arrange
        RefreshTokenUseCase useCase =
            CreateUseCase(
                new FakeRefreshTokenRepository(null));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(
                new RefreshTokenRequest(string.Empty)));
    }

    private static RefreshTokenUseCase CreateUseCase(
        FakeRefreshTokenRepository refreshTokenRepository,
        DateTimeOffset? now = null,
        Credential? credential = null)
    {
        DateTimeOffset currentTime =
            now ??
            new DateTimeOffset(
                2026,
                9,
                26,
                10,
                0,
                0,
                TimeSpan.Zero);

        credential ??=
            new Credential(
                refreshTokenRepository.Token?.CredentialId
                    ?? Guid.NewGuid(),
                Guid.NewGuid(),
                "user@example.com",
                "password-hash",
                UserRole.Customer,
                currentTime.AddDays(-10));

        return new RefreshTokenUseCase(
            refreshTokenRepository,
            new FakeCredentialRepository(credential),
            new FakeSecureTokenGenerator("new-token"),
            new FakeTokenHasher("token-hash", "new-hash"),
            new FakeTokenService("access-token"),
            new FakeDateTimeProvider(currentTime),
            new FakeRefreshTokenLifetimeProvider(
                TimeSpan.FromDays(30)),
            new FakeAuthUnitOfWork());
    }

    private sealed class FakeRefreshTokenRepository
        : IRefreshTokenRepository
    {
        public FakeRefreshTokenRepository(
            RefreshToken? token)
        {
            Token = token;
        }

        public RefreshToken? Token { get; }

        public List<RefreshToken> AddedTokens { get; } = [];

        public Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            if (Token is null)
            {
                return Task.FromResult<RefreshToken?>(null);
            }

            return Task.FromResult(
                Token.TokenHash == tokenHash
                    ? Token
                    : null);
        }

        public Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            AddedTokens.Add(refreshToken);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCredentialRepository
        : ICredentialRepository
    {
        private readonly Credential _credential;

        public FakeCredentialRepository(
            Credential credential)
        {
            _credential = credential;
        }

        public Task<Credential?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Credential?>(
                id == _credential.Id
                    ? _credential
                    : null);
        }

        public Task<Credential?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Credential?>(_credential);
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task AddAsync(
            Credential credential,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Credential credential,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecureTokenGenerator
        : ISecureTokenGenerator
    {
        private readonly string _token;

        public FakeSecureTokenGenerator(string token)
        {
            _token = token;
        }

        public string Generate()
        {
            return _token;
        }
    }

    private sealed class FakeTokenHasher
        : ITokenHasher
    {
        private readonly string _expectedInputHash;
        private readonly string _newTokenHash;

        public FakeTokenHasher(
            string expectedInputHash,
            string newTokenHash)
        {
            _expectedInputHash = expectedInputHash;
            _newTokenHash = newTokenHash;
        }

        public string Hash(string token)
        {
            if (token == "old-raw-token")
            {
                return _expectedInputHash;
            }

            return _newTokenHash;
        }
    }

    private sealed class FakeTokenService
        : ITokenService
    {
        private readonly string _accessToken;

        public FakeTokenService(string accessToken)
        {
            _accessToken = accessToken;
        }

        public string CreateAccessToken(
            Guid userId,
            UserRole role)
        {
            return _accessToken;
        }
    }

    private sealed class FakeDateTimeProvider
        : IDateTimeProvider
    {
        public FakeDateTimeProvider(
            DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeRefreshTokenLifetimeProvider
        : IRefreshTokenLifetimeProvider
    {
        public FakeRefreshTokenLifetimeProvider(
            TimeSpan lifetime)
        {
            Lifetime = lifetime;
        }

        public TimeSpan Lifetime { get; }
    }

    private sealed class FakeAuthUnitOfWork
        : IAuthUnitOfWork
    {
        public bool SaveChangesCalled { get; private set; }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;

            return Task.CompletedTask;
        }
    }
}