using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);
}