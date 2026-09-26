using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Application.Abstractions.Persistence;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Infrastructure.Auditing;
using CommerceX.Auth.Infrastructure.Persistence;
using CommerceX.Auth.Infrastructure.Persistence.Repositories;
using CommerceX.Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceX.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("AuthDatabase")));

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<
            IAuthUnitOfWork,
            AuthUnitOfWork>();

        services.AddScoped<
            ICredentialRepository,
            CredentialRepository>();

        services.AddScoped<
            IRefreshTokenRepository,
            RefreshTokenRepository>();

        services.AddScoped<
            IPasswordResetTokenRepository,
            PasswordResetTokenRepository>();

        services.AddSingleton<
            IPasswordHasher,
            PasswordHasher>();

        services.AddSingleton<
            ISecureTokenGenerator,
            SecureTokenGenerator>();

        services.AddSingleton<
            ITokenHasher,
            TokenHasher>();

        services.Configure<JwtOptions>(
            configuration.GetSection(
                JwtOptions.SectionName));

        services.AddSingleton<
            ITokenService,
            JwtTokenService>();

        services.Configure<RefreshTokenOptions>(
            configuration.GetSection(
                RefreshTokenOptions.SectionName));

        services.AddSingleton<
            IRefreshTokenLifetimeProvider,
            RefreshTokenLifetimeProvider>();

        services.Configure<PasswordResetTokenOptions>(
            configuration.GetSection(
                PasswordResetTokenOptions.SectionName));

        services.AddSingleton<
            IPasswordResetTokenLifetimeProvider,
            PasswordResetTokenLifetimeProvider>();

        return services;
    }
}