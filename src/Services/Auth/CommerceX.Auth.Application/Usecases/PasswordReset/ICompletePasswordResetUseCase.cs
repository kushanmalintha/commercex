using CommerceX.Auth.Application.Contracts.PasswordReset;

namespace CommerceX.Auth.Application.UseCases.PasswordReset;

public interface ICompletePasswordResetUseCase
{
    Task<CompletePasswordResetResponse> ExecuteAsync(
        CompletePasswordResetRequest request,
        CancellationToken cancellationToken = default);
}