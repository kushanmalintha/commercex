using CommerceX.Auth.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace CommerceX.Auth.Infrastructure.Security;

public sealed class PasswordResetTokenLifetimeProvider
    : IPasswordResetTokenLifetimeProvider
{
    private readonly PasswordResetTokenOptions _options;

    public PasswordResetTokenLifetimeProvider(
        IOptions<PasswordResetTokenOptions> options)
    {
        _options = options.Value;

        ValidateOptions();
    }

    public TimeSpan Lifetime =>
        TimeSpan.FromMinutes(_options.LifetimeMinutes);

    private void ValidateOptions()
    {
        if (_options.LifetimeMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Password-reset token lifetime must be greater than zero.");
        }
    }
}