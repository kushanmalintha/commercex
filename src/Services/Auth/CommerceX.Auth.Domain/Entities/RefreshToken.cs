namespace CommerceX.Auth.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }

    public Guid CredentialId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(
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

    public bool IsRevoked()
    {
        return RevokedAt.HasValue;
    }

    public bool IsActive(DateTimeOffset now)
    {
        return !IsExpired(now) && !IsRevoked();
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = revokedAt;
    }

    public void MarkReplacedBy(Guid replacementTokenId, DateTimeOffset revokedAt)
    {
        ReplacedByTokenId = replacementTokenId;
        Revoke(revokedAt);
    }
}