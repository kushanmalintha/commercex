using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.Application.Contracts.Registration;

public sealed record RegisterUserRequest(
    string Email,
    string Password);