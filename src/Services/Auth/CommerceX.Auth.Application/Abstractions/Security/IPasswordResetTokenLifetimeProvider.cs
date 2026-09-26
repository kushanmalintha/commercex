namespace CommerceX.Auth.Application.Abstractions.Security;

public interface IPasswordResetTokenLifetimeProvider
{
    TimeSpan Lifetime { get; }
}