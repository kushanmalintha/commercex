using CommerceX.Auth.Application.Contracts.PasswordReset;

namespace CommerceX.Auth.Application.UseCases.PasswordReset;

public interface IRequestPasswordResetUseCase
{
    Task<RequestPasswordResetResponse> ExecuteAsync(
        RequestPasswordResetRequest request,
        CancellationToken cancellationToken = default);
}