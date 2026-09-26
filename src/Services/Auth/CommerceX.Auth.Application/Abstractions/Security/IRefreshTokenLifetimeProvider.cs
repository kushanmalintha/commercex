namespace CommerceX.Auth.Application.Abstractions.Security;

public interface IRefreshTokenLifetimeProvider
{
    TimeSpan Lifetime { get; }
}