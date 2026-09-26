namespace CommerceX.Auth.Infrastructure.Security;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int LifetimeDays { get; set; } = 30;
}