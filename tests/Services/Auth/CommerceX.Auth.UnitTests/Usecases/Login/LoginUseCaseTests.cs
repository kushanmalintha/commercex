using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.Login;
using CommerceX.Auth.Application.UseCases.Login;
using CommerceX.Auth.Domain.Entities;
using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.UnitTests.UseCases.Login;

public sealed class LoginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnLoginResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        var credential = new Credential(
            credentialId,
            userId,
            "customer@example.com",
            "hashed-password",
            UserRole.Customer,
            now);

        var credentialRepository = new FakeCredentialRepository(credential);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordHasher = new FakePasswordHasher(isValid: true);
        var tokenService = new FakeTokenService("test-access-token");
        var secureTokenGenerator = new FakeSecureTokenGenerator("test-refresh-token");
        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var lifetimeProvider = new FakeRefreshTokenLifetimeProvider(
            TimeSpan.FromDays(30));
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LoginUseCase(
            credentialRepository,
            refreshTokenRepository,
            passwordHasher,
            tokenService,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new LoginRequest(
            "customer@example.com",
            "CorrectPassword123!");

        // Act
        LoginResponse result =
            await useCase.ExecuteAsync(request);

        // Assert
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("test-refresh-token", result.RefreshToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("customer@example.com", result.Email);

        Assert.NotNull(refreshTokenRepository.AddedRefreshToken);
        Assert.Equal(credentialId, refreshTokenRepository.AddedRefreshToken!.CredentialId);
        Assert.Equal(
            "hashed:test-refresh-token",
            refreshTokenRepository.AddedRefreshToken.TokenHash);
        Assert.Equal(
            now.AddDays(30),
            refreshTokenRepository.AddedRefreshToken.ExpiresAt);
        Assert.Equal(
            now,
            refreshTokenRepository.AddedRefreshToken.CreatedAt);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeEmail_WhenEmailContainsWhitespaceAndUppercaseCharacters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        var credential = new Credential(
            credentialId,
            userId,
            "customer@example.com",
            "hashed-password",
            UserRole.Customer,
            now);

        var credentialRepository = new FakeCredentialRepository(credential);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordHasher = new FakePasswordHasher(isValid: true);
        var tokenService = new FakeTokenService("test-access-token");
        var secureTokenGenerator = new FakeSecureTokenGenerator("test-refresh-token");
        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var lifetimeProvider = new FakeRefreshTokenLifetimeProvider(
            TimeSpan.FromDays(30));
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LoginUseCase(
            credentialRepository,
            refreshTokenRepository,
            passwordHasher,
            tokenService,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new LoginRequest(
            "  CUSTOMER@EXAMPLE.COM  ",
            "CorrectPassword123!");

        // Act
        LoginResponse result =
            await useCase.ExecuteAsync(request);

        // Assert
        Assert.Equal("customer@example.com", result.Email);
        Assert.Equal(
            "customer@example.com",
            credentialRepository.LastRequestedEmail);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        var credential = new Credential(
            credentialId,
            userId,
            "customer@example.com",
            "hashed-password",
            UserRole.Customer,
            now);

        var credentialRepository = new FakeCredentialRepository(credential);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordHasher = new FakePasswordHasher(isValid: false);
        var tokenService = new FakeTokenService("test-access-token");
        var secureTokenGenerator = new FakeSecureTokenGenerator("test-refresh-token");
        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var lifetimeProvider = new FakeRefreshTokenLifetimeProvider(
            TimeSpan.FromDays(30));
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LoginUseCase(
            credentialRepository,
            refreshTokenRepository,
            passwordHasher,
            tokenService,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new LoginRequest(
            "customer@example.com",
            "WrongPassword");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "Invalid email or password.",
            exception.Message);

        Assert.Null(refreshTokenRepository.AddedRefreshToken);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenEmailDoesNotExist()
    {
        // Arrange
        var credentialRepository = new FakeCredentialRepository(null);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordHasher = new FakePasswordHasher(isValid: true);
        var tokenService = new FakeTokenService("test-access-token");
        var secureTokenGenerator = new FakeSecureTokenGenerator("test-refresh-token");
        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(
            new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero));
        var lifetimeProvider = new FakeRefreshTokenLifetimeProvider(
            TimeSpan.FromDays(30));
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LoginUseCase(
            credentialRepository,
            refreshTokenRepository,
            passwordHasher,
            tokenService,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new LoginRequest(
            "unknown@example.com",
            "SomePassword123!");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "Invalid email or password.",
            exception.Message);

        Assert.Null(refreshTokenRepository.AddedRefreshToken);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorizedAccessException_WhenCredentialIsInactive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

        var credential = new Credential(
            credentialId,
            userId,
            "customer@example.com",
            "hashed-password",
            UserRole.Customer,
            now);

        credential.SetActive(false, now);

        var credentialRepository = new FakeCredentialRepository(credential);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordHasher = new FakePasswordHasher(isValid: true);
        var tokenService = new FakeTokenService("test-access-token");
        var secureTokenGenerator = new FakeSecureTokenGenerator("test-refresh-token");
        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var lifetimeProvider = new FakeRefreshTokenLifetimeProvider(
            TimeSpan.FromDays(30));
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LoginUseCase(
            credentialRepository,
            refreshTokenRepository,
            passwordHasher,
            tokenService,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new LoginRequest(
            "customer@example.com",
            "CorrectPassword123!");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => useCase.ExecuteAsync(request));

        Assert.Equal(
            "Invalid email or password.",
            exception.Message);

        Assert.Null(refreshTokenRepository.AddedRefreshToken);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenEmailIsEmpty()
    {
        // Arrange
        var credentialRepository = new FakeCredentialRepository(null);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordHasher = new FakePasswordHasher(isValid: true);
        var tokenService = new FakeTokenService("test-access-token");
        var secureTokenGenerator = new FakeSecureTokenGenerator("test-refresh-token");
        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(
            new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero));
        var lifetimeProvider = new FakeRefreshTokenLifetimeProvider(
            TimeSpan.FromDays(30));
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LoginUseCase(
            credentialRepository,
            refreshTokenRepository,
            passwordHasher,
            tokenService,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new LoginRequest(
            string.Empty,
            "Password123!");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(request));

        Assert.Null(refreshTokenRepository.AddedRefreshToken);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowArgumentException_WhenPasswordIsEmpty()
    {
        // Arrange
        var credentialRepository = new FakeCredentialRepository(null);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordHasher = new FakePasswordHasher(isValid: true);
        var tokenService = new FakeTokenService("test-access-token");
        var secureTokenGenerator = new FakeSecureTokenGenerator("test-refresh-token");
        var tokenHasher = new FakeTokenHasher();
        var dateTimeProvider = new FakeDateTimeProvider(
            new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero));
        var lifetimeProvider = new FakeRefreshTokenLifetimeProvider(
            TimeSpan.FromDays(30));
        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new LoginUseCase(
            credentialRepository,
            refreshTokenRepository,
            passwordHasher,
            tokenService,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new LoginRequest(
            "customer@example.com",
            string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(request));

        Assert.Null(refreshTokenRepository.AddedRefreshToken);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private sealed class FakeCredentialRepository : ICredentialRepository
    {
        private readonly Credential? _credential;

        public string? LastRequestedEmail { get; private set; }

        public FakeCredentialRepository(Credential? credential)
        {
            _credential = credential;
        }

        public Task<Credential?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Credential?>(null);
        }

        public Task<Credential?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            LastRequestedEmail = email;
            return Task.FromResult(_credential);
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
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

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public RefreshToken? AddedRefreshToken { get; private set; }

        public Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<RefreshToken?>(null);
        }

        public Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            AddedRefreshToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        private readonly bool _isValid;

        public FakePasswordHasher(bool isValid)
        {
            _isValid = isValid;
        }

        public string Hash(string password)
        {
            return "hashed-password";
        }

        public bool Verify(
            string password,
            string passwordHash)
        {
            return _isValid;
        }
    }

    private sealed class FakeTokenService : ITokenService
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

    private sealed class FakeSecureTokenGenerator : ISecureTokenGenerator
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

        public FakeDateTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }
    }

    private sealed class FakeRefreshTokenLifetimeProvider
        : IRefreshTokenLifetimeProvider
    {
        public TimeSpan Lifetime { get; }

        public FakeRefreshTokenLifetimeProvider(TimeSpan lifetime)
        {
            Lifetime = lifetime;
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