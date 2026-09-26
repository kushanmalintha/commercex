using CommerceX.Auth.Application.Contracts.Refresh;

namespace CommerceX.Auth.Application.UseCases.Refresh;

public interface IRefreshTokenUseCase
{
    Task<RefreshTokenResponse> ExecuteAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);
}