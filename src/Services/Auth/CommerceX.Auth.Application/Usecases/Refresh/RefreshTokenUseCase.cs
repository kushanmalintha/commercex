using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.Refresh;
using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.UseCases.Refresh;

public sealed class RefreshTokenUseCase : IRefreshTokenUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRefreshTokenLifetimeProvider _lifetimeProvider;
    private readonly IAuthUnitOfWork _unitOfWork;

    public RefreshTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        ICredentialRepository credentialRepository,
        ISecureTokenGenerator secureTokenGenerator,
        ITokenHasher tokenHasher,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        IRefreshTokenLifetimeProvider lifetimeProvider,
        IAuthUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _credentialRepository = credentialRepository;
        _secureTokenGenerator = secureTokenGenerator;
        _tokenHasher = tokenHasher;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
        _lifetimeProvider = lifetimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshTokenResponse> ExecuteAsync(
        RefreshTokenRequest request,
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

        DateTimeOffset now =
            _dateTimeProvider.UtcNow;

        if (!refreshToken.IsActive(now))
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        var credential =
            await _credentialRepository.GetByIdAsync(
                refreshToken.CredentialId,
                cancellationToken);

        if (credential is null || !credential.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        string newAccessToken =
            _tokenService.CreateAccessToken(
                credential.UserId,
                credential.Role);

        string newRawRefreshToken =
            _secureTokenGenerator.Generate();

        string newRefreshTokenHash =
            _tokenHasher.Hash(newRawRefreshToken);

        Guid newRefreshTokenId =
            Guid.NewGuid();

        RefreshToken replacementToken =
            new(
                newRefreshTokenId,
                credential.Id,
                newRefreshTokenHash,
                now.Add(_lifetimeProvider.Lifetime),
                now);

        refreshToken.MarkReplacedBy(
            replacementToken.Id,
            now);

        await _refreshTokenRepository.UpdateAsync(
            refreshToken,
            cancellationToken);

        await _refreshTokenRepository.AddAsync(
            replacementToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new RefreshTokenResponse(
            newAccessToken,
            newRawRefreshToken,
            "Bearer",
            credential.UserId,
            credential.Email);
    }
}