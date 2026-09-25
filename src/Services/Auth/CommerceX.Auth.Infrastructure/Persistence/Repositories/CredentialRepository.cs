using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommerceX.Auth.Infrastructure.Persistence.Repositories;

public sealed class CredentialRepository : ICredentialRepository
{
    private readonly AuthDbContext _dbContext;

    public CredentialRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Credential?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Credentials
            .FirstOrDefaultAsync(
                credential => credential.Id == id,
                cancellationToken);
    }

    public async Task<Credential?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Credentials
            .FirstOrDefaultAsync(
                credential => credential.Email == email,
                cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Credentials
            .AnyAsync(
                credential => credential.Email == email,
                cancellationToken);
    }

    public async Task AddAsync(
        Credential credential,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Credentials.AddAsync(
            credential,
            cancellationToken);
    }

    public Task UpdateAsync(
        Credential credential,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Credentials.Update(credential);

        return Task.CompletedTask;
    }
}