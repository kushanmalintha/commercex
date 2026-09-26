using CommerceX.Auth.Application.Contracts.Login;

namespace CommerceX.Auth.Application.UseCases.Login;

public interface ILoginUseCase
{
    Task<LoginResponse> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}