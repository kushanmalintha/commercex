using CommerceX.Auth.Application.Contracts.Registration;

namespace CommerceX.Auth.Application.UseCases.Registration;

public interface IRegisterUserUseCase
{
    Task<RegisterUserResponse> ExecuteAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);
}