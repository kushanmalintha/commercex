using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.Logout;
using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.UseCases.Logout;

public sealed class LogoutUseCase : ILogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthUnitOfWork _unitOfWork;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IAuthUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<LogoutResponse> ExecuteAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ArgumentException(
                "Refresh token is required.",
                nameof(request));
        }

        string tokenHash =
            _tokenHasher.Hash(request.RefreshToken);

        RefreshToken? refreshToken =
            await _refreshTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        if (refreshToken is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        DateTimeOffset now = _dateTimeProvider.UtcNow;

        if (!refreshToken.IsActive(now))
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        refreshToken.Revoke(now);

        await _refreshTokenRepository.UpdateAsync(
            refreshToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new LogoutResponse(true);
    }
}