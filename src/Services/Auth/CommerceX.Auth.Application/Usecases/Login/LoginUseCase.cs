using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.Login;
using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.UseCases.Login;

public sealed class LoginUseCase : ILoginUseCase
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRefreshTokenLifetimeProvider _lifetimeProvider;
    private readonly IAuthUnitOfWork _unitOfWork;

    public LoginUseCase(
        ICredentialRepository credentialRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ISecureTokenGenerator secureTokenGenerator,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IRefreshTokenLifetimeProvider lifetimeProvider,
        IAuthUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _secureTokenGenerator = secureTokenGenerator;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _lifetimeProvider = lifetimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponse> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException(
                "Password is required.",
                nameof(request));
        }

        string normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        var credential =
            await _credentialRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (credential is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (!credential.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        bool passwordValid =
            _passwordHasher.Verify(
                request.Password,
                credential.PasswordHash);

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        DateTimeOffset now =
            _dateTimeProvider.UtcNow;

        string accessToken =
            _tokenService.CreateAccessToken(
                credential.UserId,
                credential.Role);

        string rawRefreshToken =
            _secureTokenGenerator.Generate();

        string refreshTokenHash =
            _tokenHasher.Hash(rawRefreshToken);

        RefreshToken refreshToken =
            new(
                Guid.NewGuid(),
                credential.Id,
                refreshTokenHash,
                now.Add(_lifetimeProvider.Lifetime),
                now);

        await _refreshTokenRepository.AddAsync(
            refreshToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new LoginResponse(
            accessToken,
            rawRefreshToken,
            "Bearer",
            credential.UserId,
            credential.Email);
    }
}