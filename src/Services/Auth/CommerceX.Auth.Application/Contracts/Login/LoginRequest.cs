namespace CommerceX.Auth.Application.Contracts.Login;

public sealed record LoginRequest(
    string Email,
    string Password);