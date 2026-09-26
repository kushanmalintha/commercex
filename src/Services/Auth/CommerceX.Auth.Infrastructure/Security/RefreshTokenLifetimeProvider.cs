using CommerceX.Auth.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace CommerceX.Auth.Infrastructure.Security;

public sealed class RefreshTokenLifetimeProvider
    : IRefreshTokenLifetimeProvider
{
    private readonly RefreshTokenOptions _options;

    public RefreshTokenLifetimeProvider(
        IOptions<RefreshTokenOptions> options)
    {
        _options = options.Value;

        ValidateOptions();
    }

    public TimeSpan Lifetime =>
        TimeSpan.FromDays(_options.LifetimeDays);

    private void ValidateOptions()
    {
        if (_options.LifetimeDays <= 0)
        {
            throw new InvalidOperationException(
                "Refresh-token lifetime must be greater than zero.");
        }
    }
}