using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.Application.Contracts.Registration;

public sealed record RegisterUserResponse(
    Guid CredentialId,
    Guid UserId,
    string Email,
    UserRole Role);