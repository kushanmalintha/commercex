namespace CommerceX.Auth.Infrastructure.Security;

public sealed class PasswordResetTokenOptions
{
    public const string SectionName = "PasswordResetToken";

    public int LifetimeMinutes { get; set; } = 30;
}