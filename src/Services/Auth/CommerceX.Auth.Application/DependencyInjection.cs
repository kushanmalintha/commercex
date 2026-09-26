using CommerceX.Auth.Application.UseCases.Login;
using CommerceX.Auth.Application.UseCases.Logout;
using CommerceX.Auth.Application.UseCases.PasswordReset;
using CommerceX.Auth.Application.UseCases.Refresh;
using CommerceX.Auth.Application.UseCases.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceX.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IRegisterUserUseCase,
            RegisterUserUseCase>();

        services.AddScoped<
            ILoginUseCase,
            LoginUseCase>();

        services.AddScoped<
            IRefreshTokenUseCase,
            RefreshTokenUseCase>();

        services.AddScoped<
            ILogoutUseCase,
            LogoutUseCase>();

        services.AddScoped<
            IRequestPasswordResetUseCase,
            RequestPasswordResetUseCase>();

        services.AddScoped<
            ICompletePasswordResetUseCase,
            CompletePasswordResetUseCase>();

        return services;
    }
}