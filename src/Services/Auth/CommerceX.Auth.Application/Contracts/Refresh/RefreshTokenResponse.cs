namespace CommerceX.Auth.Application.Contracts.Refresh;

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    Guid UserId,
    string Email);