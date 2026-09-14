namespace CommerceX.Auth.Domain.Entities;

public sealed class PasswordResetToken
{
    public Guid Id { get; private set; }

    public Guid CredentialId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    private PasswordResetToken()
    {
        TokenHash = string.Empty;
    }

    public PasswordResetToken(
        Guid id,
        Guid credentialId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        CredentialId = credentialId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return now >= ExpiresAt;
    }

    public bool IsUsed()
    {
        return UsedAt.HasValue;
    }

    public bool IsActive(DateTimeOffset now)
    {
        return !IsExpired(now) && !IsUsed();
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        if (UsedAt.HasValue)
        {
            return;
        }

        UsedAt = usedAt;
    }
}