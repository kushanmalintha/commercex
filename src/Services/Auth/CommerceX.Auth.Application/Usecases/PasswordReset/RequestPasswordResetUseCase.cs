using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.PasswordReset;
using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.UseCases.PasswordReset;

public sealed class RequestPasswordResetUseCase
    : IRequestPasswordResetUseCase
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordResetTokenLifetimeProvider _lifetimeProvider;
    private readonly IAuthUnitOfWork _unitOfWork;

    public RequestPasswordResetUseCase(
        ICredentialRepository credentialRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ISecureTokenGenerator secureTokenGenerator,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IPasswordResetTokenLifetimeProvider lifetimeProvider,
        IAuthUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _secureTokenGenerator = secureTokenGenerator;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _lifetimeProvider = lifetimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<RequestPasswordResetResponse> ExecuteAsync(
        RequestPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(request));
        }

        string email = request.Email.Trim().ToLowerInvariant();

        Credential? credential =
            await _credentialRepository.GetByEmailAsync(
                email,
                cancellationToken);

        /*
         * Do not reveal whether the account exists.
         *
         * The API layer can return the same response regardless
         * of whether a credential was found.
         */
        if (credential is null || !credential.IsActive)
        {
            return new RequestPasswordResetResponse(true);
        }

        DateTimeOffset now = _dateTimeProvider.UtcNow;

        string rawResetToken =
            _secureTokenGenerator.Generate();

        string tokenHash =
            _tokenHasher.Hash(rawResetToken);

        PasswordResetToken resetToken =
            new(
                Guid.NewGuid(),
                credential.Id,
                tokenHash,
                now.Add(_lifetimeProvider.Lifetime),
                now);

        await _passwordResetTokenRepository.AddAsync(
            resetToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        /*
         * The raw token is intentionally not returned.
         *
         * A future notification/email mechanism will deliver the
         * reset token to the user.
         */
        return new RequestPasswordResetResponse(true);
    }
}