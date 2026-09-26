using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.PasswordReset;
using CommerceX.Auth.Application.UseCases.PasswordReset;
using CommerceX.Auth.Domain.Entities;
using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.UnitTests.Application.UseCases;

public sealed class CompletePasswordResetUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidToken_UpdatesPasswordAndConsumesToken()
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

        var credential = CreateCredential();

        var resetToken = new PasswordResetToken(
            Guid.NewGuid(),
            credential.Id,
            "HASHED:valid-reset-token",
            now.AddMinutes(30),
            now.AddMinutes(-5));

        var credentialRepository = new FakeCredentialRepository
        {
            Credential = credential
        };

        var passwordResetTokenRepository =
            new FakePasswordResetTokenRepository
            {
                Token = resetToken
            };

        var passwordHasher = new FakePasswordHasher();

        var tokenHasher = new FakeTokenHasher();

        var dateTimeProvider = new FakeDateTimeProvider
        {
            UtcNowValue = now
        };

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new CompletePasswordResetUseCase(
            passwordResetTokenRepository,
            credentialRepository,
            passwordHasher,
            tokenHasher,
            dateTimeProvider,
            unitOfWork);

        var request = new CompletePasswordResetRequest(
            "valid-reset-token",
            "NewSecurePassword123!");

        // Act
        var response = await useCase.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);

        Assert.Equal(
            "HASHED_PASSWORD:NewSecurePassword123!",
            credential.PasswordHash);

        Assert.Equal(
            now,
            credential.PasswordChangedAt);

        Assert.False(resetToken.IsActive(now));

        Assert.Equal(
            resetToken,
            passwordResetTokenRepository.UpdatedToken);

        Assert.Equal(
            credential,
            credentialRepository.UpdatedCredential);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCallCount);

        Assert.Equal(
            1,
            passwordHasher.HashCallCount);

        Assert.Equal(
            1,
            tokenHasher.HashCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var resetTokenRepository =
            new FakePasswordResetTokenRepository
            {
                Token = null
            };

        var useCase = new CompletePasswordResetUseCase(
            resetTokenRepository,
            new FakeCredentialRepository(),
            new FakePasswordHasher(),
            new FakeTokenHasher(),
            new FakeDateTimeProvider(),
            new FakeAuthUnitOfWork());

        var request = new CompletePasswordResetRequest(
            "invalid-reset-token",
            "NewSecurePassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredToken_ThrowsUnauthorizedAccessException()
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

        var credential = CreateCredential();

        var expiredToken = new PasswordResetToken(
            Guid.NewGuid(),
            credential.Id,
            "HASHED:expired-reset-token",
            now.AddMinutes(-1),
            now.AddMinutes(-31));

        var resetTokenRepository =
            new FakePasswordResetTokenRepository
            {
                Token = expiredToken
            };

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new CompletePasswordResetUseCase(
            resetTokenRepository,
            new FakeCredentialRepository
            {
                Credential = credential
            },
            new FakePasswordHasher(),
            new FakeTokenHasher(),
            new FakeDateTimeProvider
            {
                UtcNowValue = now
            },
            unitOfWork);

        var request = new CompletePasswordResetRequest(
            "expired-reset-token",
            "NewSecurePassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(request));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyUsedToken_ThrowsUnauthorizedAccessException()
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

        var credential = CreateCredential();

        var usedToken = new PasswordResetToken(
            Guid.NewGuid(),
            credential.Id,
            "HASHED:used-reset-token",
            now.AddMinutes(30),
            now.AddMinutes(-5));

        usedToken.MarkUsed(now.AddMinutes(-1));

        var resetTokenRepository =
            new FakePasswordResetTokenRepository
            {
                Token = usedToken
            };

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new CompletePasswordResetUseCase(
            resetTokenRepository,
            new FakeCredentialRepository
            {
                Credential = credential
            },
            new FakePasswordHasher(),
            new FakeTokenHasher(),
            new FakeDateTimeProvider
            {
                UtcNowValue = now
            },
            unitOfWork);

        var request = new CompletePasswordResetRequest(
            "used-reset-token",
            "NewSecurePassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(request));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithInactiveCredential_ThrowsUnauthorizedAccessException()
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

        var credential = CreateCredential();

        credential.SetActive(false, now);

        var resetToken = new PasswordResetToken(
            Guid.NewGuid(),
            credential.Id,
            "HASHED:reset-token",
            now.AddMinutes(30),
            now.AddMinutes(-5));

        var credentialRepository =
            new FakeCredentialRepository
            {
                Credential = credential
            };

        var resetTokenRepository =
            new FakePasswordResetTokenRepository
            {
                Token = resetToken
            };

        var unitOfWork = new FakeAuthUnitOfWork();

        var useCase = new CompletePasswordResetUseCase(
            resetTokenRepository,
            credentialRepository,
            new FakePasswordHasher(),
            new FakeTokenHasher(),
            new FakeDateTimeProvider
            {
                UtcNowValue = now
            },
            unitOfWork);

        var request = new CompletePasswordResetRequest(
            "reset-token",
            "NewSecurePassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(request));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingResetToken_ThrowsArgumentException()
    {
        // Arrange
        var useCase = new CompletePasswordResetUseCase(
            new FakePasswordResetTokenRepository(),
            new FakeCredentialRepository(),
            new FakePasswordHasher(),
            new FakeTokenHasher(),
            new FakeDateTimeProvider(),
            new FakeAuthUnitOfWork());

        var request = new CompletePasswordResetRequest(
            string.Empty,
            "NewSecurePassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingNewPassword_ThrowsArgumentException()
    {
        // Arrange
        var useCase = new CompletePasswordResetUseCase(
            new FakePasswordResetTokenRepository(),
            new FakeCredentialRepository(),
            new FakePasswordHasher(),
            new FakeTokenHasher(),
            new FakeDateTimeProvider(),
            new FakeAuthUnitOfWork());

        var request = new CompletePasswordResetRequest(
            "reset-token",
            string.Empty);

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
            "OLD_PASSWORD_HASH",
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

        public Credential? UpdatedCredential { get; private set; }

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
            UpdatedCredential = credential;
            Credential = credential;

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordResetTokenRepository
        : IPasswordResetTokenRepository
    {
        public PasswordResetToken? Token { get; set; }

        public PasswordResetToken? UpdatedToken { get; private set; }

        public Task<PasswordResetToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            if (Token is null)
            {
                return Task.FromResult<PasswordResetToken?>(null);
            }

            return Task.FromResult(
                Token.TokenHash == tokenHash
                    ? Token
                    : null);
        }

        public Task AddAsync(
            PasswordResetToken passwordResetToken,
            CancellationToken cancellationToken = default)
        {
            Token = passwordResetToken;

            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            PasswordResetToken passwordResetToken,
            CancellationToken cancellationToken = default)
        {
            UpdatedToken = passwordResetToken;
            Token = passwordResetToken;

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public int HashCallCount { get; private set; }

        public string Hash(string password)
        {
            HashCallCount++;

            return $"HASHED_PASSWORD:{password}";
        }

        public bool Verify(
            string password,
            string passwordHash)
        {
            return passwordHash == $"HASHED_PASSWORD:{password}";
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