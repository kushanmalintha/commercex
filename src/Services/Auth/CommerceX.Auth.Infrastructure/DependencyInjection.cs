using CommerceX.Auth.Application.Abstractions.Auditing;
using CommerceX.Auth.Infrastructure.Auditing;
using CommerceX.Auth.Infrastructure.Persistence;
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

        return services;
    }
}