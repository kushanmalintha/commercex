using CommerceX.Auth.Application.Contracts.Registration;
using CommerceX.Auth.Application.UseCases.Registration;
using Microsoft.AspNetCore.Mvc;
using CommerceX.Auth.Application.Contracts.Login;
using CommerceX.Auth.Application.UseCases.Login;
using CommerceX.Auth.Application.Contracts.Refresh;
using CommerceX.Auth.Application.UseCases.Refresh;
using CommerceX.Auth.Application.Contracts.Logout;
using CommerceX.Auth.Application.UseCases.Logout;
using CommerceX.Auth.Application.Contracts.PasswordReset;
using CommerceX.Auth.Application.UseCases.PasswordReset;

namespace CommerceX.Auth.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRegisterUserUseCase _registerUserUseCase;
    private readonly ILoginUseCase _loginUseCase;
    private readonly IRefreshTokenUseCase _refreshTokenUseCase;
    private readonly ILogoutUseCase _logoutUseCase;
    private readonly IRequestPasswordResetUseCase _requestPasswordResetUseCase;
    private readonly ICompletePasswordResetUseCase _completePasswordResetUseCase;

    public AuthController(
        IRegisterUserUseCase registerUserUseCase,
        ILoginUseCase loginUseCase,
        IRefreshTokenUseCase refreshTokenUseCase,
        ILogoutUseCase logoutUseCase,
        IRequestPasswordResetUseCase requestPasswordResetUseCase,
        ICompletePasswordResetUseCase completePasswordResetUseCase)
    {
        _registerUserUseCase = registerUserUseCase;
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
        _logoutUseCase = logoutUseCase;
        _requestPasswordResetUseCase = requestPasswordResetUseCase;
        _completePasswordResetUseCase = completePasswordResetUseCase;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _registerUserUseCase.ExecuteAsync(
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _loginUseCase.ExecuteAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(
        typeof(RefreshTokenResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _refreshTokenUseCase.ExecuteAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(
        typeof(LogoutResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<LogoutResponse>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _logoutUseCase.ExecuteAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("password-reset/request")]
    [ProducesResponseType(
        typeof(RequestPasswordResetResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestPasswordResetResponse>> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _requestPasswordResetUseCase.ExecuteAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("password-reset/complete")]
    [ProducesResponseType(
        typeof(CompletePasswordResetResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CompletePasswordResetResponse>> CompletePasswordReset(
        [FromBody] CompletePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _completePasswordResetUseCase.ExecuteAsync(
            request,
            cancellationToken);

        return Ok(response);
    }
}