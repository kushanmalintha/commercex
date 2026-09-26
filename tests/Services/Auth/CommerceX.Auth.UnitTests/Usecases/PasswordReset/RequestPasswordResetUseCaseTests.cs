using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.PasswordReset;
using CommerceX.Auth.Application.UseCases.PasswordReset;
using CommerceX.Auth.Domain.Entities;
using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.UnitTests.Application.UseCases;

public sealed class RequestPasswordResetUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithExistingActiveCredential_CreatesPasswordResetToken()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);
        var credential = CreateCredential();

        var credentialRepository = new FakeCredentialRepository
        {
            Credential = credential
        };

        var passwordResetTokenRepository = new FakePasswordResetTokenRepository();

        var secureTokenGenerator = new FakeSecureTokenGenerator
        {
            Token = "raw-reset-token"
        };

        var tokenHasher = new FakeTokenHasher();

        var dateTimeProvider = new FakeDateTimeProvider
        {
            UtcNowValue = now
        };

        var lifetimeProvider = new FakePasswordResetTokenLifetimeProvider
        {
            Lifetime = TimeSpan.FromMinutes(30)
        };

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new RequestPasswordResetUseCase(
            credentialRepository,
            passwordResetTokenRepository,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new RequestPasswordResetRequest(
            "  USER@Example.COM  ");

        // Act
        var response = await useCase.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);

        Assert.NotNull(passwordResetTokenRepository.AddedToken);

        Assert.Equal(
            credential.Id,
            passwordResetTokenRepository.AddedToken!.CredentialId);

        Assert.Equal(
            "HASHED:raw-reset-token",
            passwordResetTokenRepository.AddedToken.TokenHash);

        Assert.Equal(
            now.AddMinutes(30),
            passwordResetTokenRepository.AddedToken.ExpiresAt);

        Assert.Equal(
            now,
            passwordResetTokenRepository.AddedToken.CreatedAt);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(1, secureTokenGenerator.GenerateCallCount);
        Assert.Equal(1, tokenHasher.HashCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownEmail_ReturnsSuccessWithoutCreatingToken()
    {
        // Arrange
        var credentialRepository = new FakeCredentialRepository
        {
            Credential = null
        };

        var passwordResetTokenRepository = new FakePasswordResetTokenRepository();

        var secureTokenGenerator = new FakeSecureTokenGenerator();
        var tokenHasher = new FakeTokenHasher();

        var dateTimeProvider = new FakeDateTimeProvider
        {
            UtcNowValue = DateTimeOffset.UtcNow
        };

        var lifetimeProvider = new FakePasswordResetTokenLifetimeProvider();

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new RequestPasswordResetUseCase(
            credentialRepository,
            passwordResetTokenRepository,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new RequestPasswordResetRequest(
            "unknown@example.com");

        // Act
        var response = await useCase.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);

        Assert.Null(passwordResetTokenRepository.AddedToken);

        Assert.Equal(0, secureTokenGenerator.GenerateCallCount);
        Assert.Equal(0, tokenHasher.HashCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithInactiveCredential_ReturnsSuccessWithoutCreatingToken()
    {
        // Arrange
        var credential = CreateCredential();
        credential.SetActive(false, DateTimeOffset.UtcNow);

        var credentialRepository = new FakeCredentialRepository
        {
            Credential = credential
        };

        var passwordResetTokenRepository = new FakePasswordResetTokenRepository();

        var secureTokenGenerator = new FakeSecureTokenGenerator();
        var tokenHasher = new FakeTokenHasher();

        var dateTimeProvider = new FakeDateTimeProvider
        {
            UtcNowValue = DateTimeOffset.UtcNow
        };

        var lifetimeProvider = new FakePasswordResetTokenLifetimeProvider();

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new RequestPasswordResetUseCase(
            credentialRepository,
            passwordResetTokenRepository,
            secureTokenGenerator,
            tokenHasher,
            dateTimeProvider,
            lifetimeProvider,
            unitOfWork);

        var request = new RequestPasswordResetRequest(
            "user@example.com");

        // Act
        var response = await useCase.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);

        Assert.Null(passwordResetTokenRepository.AddedToken);

        Assert.Equal(0, secureTokenGenerator.GenerateCallCount);
        Assert.Equal(0, tokenHasher.HashCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingEmail_ThrowsArgumentException()
    {
        // Arrange
        var useCase = new RequestPasswordResetUseCase(
            new FakeCredentialRepository(),
            new FakePasswordResetTokenRepository(),
            new FakeSecureTokenGenerator(),
            new FakeTokenHasher(),
            new FakeDateTimeProvider(),
            new FakePasswordResetTokenLifetimeProvider(),
            new FakeAuthUnitOfWork());

        var request = new RequestPasswordResetRequest(string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(request));
    }

    private static Credential CreateCredential()
    {
        return new Credential(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user@example.com",
            "PASSWORD_HASH",
            UserRole.Customer,
            new DateTimeOffset(
                2026,
                9,
                26,
                9,
                0,
                0,
                TimeSpan.Zero));
    }

    private sealed class FakeCredentialRepository : ICredentialRepository
    {
        public Credential? Credential { get; set; }

        public Task<Credential?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Credential?.Id == id ? Credential : null);
        }

        public Task<Credential?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Credential);
        }

        public Task<bool> ExistsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Credential is not null);
        }

        public Task AddAsync(
            Credential credential,
            CancellationToken cancellationToken = default)
        {
            Credential = credential;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Credential credential,
            CancellationToken cancellationToken = default)
        {
            Credential = credential;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordResetTokenRepository
        : IPasswordResetTokenRepository
    {
        public PasswordResetToken? AddedToken { get; private set; }

        public Task<PasswordResetToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PasswordResetToken?>(null);
        }

        public Task AddAsync(
            PasswordResetToken passwordResetToken,
            CancellationToken cancellationToken = default)
        {
            AddedToken = passwordResetToken;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            PasswordResetToken passwordResetToken,
            CancellationToken cancellationToken = default)
        {
            AddedToken = passwordResetToken;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecureTokenGenerator
        : ISecureTokenGenerator
    {
        public string Token { get; set; } = "test-token";

        public int GenerateCallCount { get; private set; }

        public string Generate()
        {
            GenerateCallCount++;
            return Token;
        }
    }

    private sealed class FakeTokenHasher : ITokenHasher
    {
        public int HashCallCount { get; private set; }

        public string Hash(string token)
        {
            HashCallCount++;
            return $"HASHED:{token}";
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNowValue { get; set; }
            = DateTimeOffset.UtcNow;

        public DateTimeOffset UtcNow => UtcNowValue;
    }

    private sealed class FakePasswordResetTokenLifetimeProvider
        : IPasswordResetTokenLifetimeProvider
    {
        public TimeSpan Lifetime { get; set; }
            = TimeSpan.FromMinutes(30);
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