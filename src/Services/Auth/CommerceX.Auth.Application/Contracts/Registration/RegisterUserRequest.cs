using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.Application.Contracts.Registration;

public sealed record RegisterUserRequest(
    Guid UserId,
    string Email,
    string Password,
    UserRole Role);