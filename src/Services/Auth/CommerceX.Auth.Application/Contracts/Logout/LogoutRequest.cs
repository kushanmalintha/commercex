namespace CommerceX.Auth.Application.Contracts.Logout;

public sealed record LogoutRequest(
    string RefreshToken);