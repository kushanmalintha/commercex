using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.PasswordReset;
using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.UseCases.PasswordReset;

public sealed class CompletePasswordResetUseCase
    : ICompletePasswordResetUseCase
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthUnitOfWork _unitOfWork;

    public CompletePasswordResetUseCase(
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ICredentialRepository credentialRepository,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IAuthUnitOfWork unitOfWork)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompletePasswordResetResponse> ExecuteAsync(
        CompletePasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ResetToken))
        {
            throw new ArgumentException(
                "Reset token is required.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException(
                "New password is required.",
                nameof(request));
        }

        string tokenHash =
            _tokenHasher.Hash(request.ResetToken);

        PasswordResetToken? resetToken =
            await _passwordResetTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        if (resetToken is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid password reset token.");
        }

        DateTimeOffset now = _dateTimeProvider.UtcNow;

        if (!resetToken.IsActive(now))
        {
            throw new UnauthorizedAccessException(
                "Invalid password reset token.");
        }

        Credential? credential =
            await _credentialRepository.GetByIdAsync(
                resetToken.CredentialId,
                cancellationToken);

        if (credential is null || !credential.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Invalid password reset token.");
        }

        string newPasswordHash =
            _passwordHasher.Hash(request.NewPassword);

        credential.UpdatePasswordHash(
            newPasswordHash,
            now);

        resetToken.MarkUsed(now);

        await _credentialRepository.UpdateAsync(
            credential,
            cancellationToken);

        await _passwordResetTokenRepository.UpdateAsync(
            resetToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CompletePasswordResetResponse(true);
    }
}