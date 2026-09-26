namespace CommerceX.Auth.Application.Contracts.PasswordReset;

public sealed record RequestPasswordResetRequest(
    string Email);