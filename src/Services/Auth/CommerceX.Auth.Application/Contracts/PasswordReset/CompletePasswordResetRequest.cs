namespace CommerceX.Auth.Application.Contracts.PasswordReset;

public sealed record CompletePasswordResetRequest(
    string ResetToken,
    string NewPassword);