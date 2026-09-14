using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.Domain.Entities;

public sealed class Credential
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Credential()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public Credential(
        Guid id,
        Guid userId,
        string email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public void UpdatePasswordHash(
        string passwordHash,
        DateTimeOffset updatedAt)
    {
        PasswordHash = passwordHash;
        PasswordChangedAt = updatedAt;
        UpdatedAt = updatedAt;
    }

    public void SetActive(
        bool isActive,
        DateTimeOffset updatedAt)
    {
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }
}