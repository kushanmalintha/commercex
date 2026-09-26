using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Messaging;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Application.Contracts.Registration;
using CommerceX.Auth.Domain.Entities;

namespace CommerceX.Auth.Application.UseCases.Registration;

public sealed class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthEventPublisher _authEventPublisher;

    public RegisterUserUseCase(
        ICredentialRepository credentialRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IAuthEventPublisher authEventPublisher)
    {
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _authEventPublisher = authEventPublisher;
    }

    public async Task<RegisterUserResponse> ExecuteAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(request));
        }

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

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        bool emailExists =
            await _credentialRepository.ExistsByEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A credential already exists for this email address.");
        }

        string passwordHash =
            _passwordHasher.Hash(request.Password);

        DateTimeOffset now = _dateTimeProvider.UtcNow;

        Credential credential = new(
            Guid.NewGuid(),
            request.UserId,
            normalizedEmail,
            passwordHash,
            request.Role,
            now);

        await _credentialRepository.AddAsync(
            credential,
            cancellationToken);

        await _authEventPublisher.PublishUserRegisteredAsync(
            credential.UserId,
            cancellationToken);

        return new RegisterUserResponse(
            credential.Id,
            credential.UserId,
            credential.Email,
            credential.Role);
    }
}