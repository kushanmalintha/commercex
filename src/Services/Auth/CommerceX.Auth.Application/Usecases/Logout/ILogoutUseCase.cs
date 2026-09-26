using CommerceX.Auth.Application.Contracts.Logout;

namespace CommerceX.Auth.Application.UseCases.Logout;

public interface ILogoutUseCase
{
    Task<LogoutResponse> ExecuteAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default);
}