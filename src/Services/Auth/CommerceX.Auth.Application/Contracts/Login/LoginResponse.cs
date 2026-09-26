namespace CommerceX.Auth.Application.Contracts.Login;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    Guid UserId,
    string Email);