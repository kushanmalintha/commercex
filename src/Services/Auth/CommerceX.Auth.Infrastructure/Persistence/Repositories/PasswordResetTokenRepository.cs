using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommerceX.Auth.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository
    : IPasswordResetTokenRepository
{
    private readonly AuthDbContext _dbContext;

    public PasswordResetTokenRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(
                passwordResetToken =>
                    passwordResetToken.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task AddAsync(
        PasswordResetToken passwordResetToken,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PasswordResetTokens.AddAsync(
            passwordResetToken,
            cancellationToken);
    }

    public Task UpdateAsync(
        PasswordResetToken passwordResetToken,
        CancellationToken cancellationToken = default)
    {
        _dbContext.PasswordResetTokens.Update(passwordResetToken);

        return Task.CompletedTask;
    }
}