using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.Abstractions.Persistence;

public interface ICredentialRepository
{
    Task<Credential?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Credential?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Credential credential,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Credential credential,
        CancellationToken cancellationToken = default);
}