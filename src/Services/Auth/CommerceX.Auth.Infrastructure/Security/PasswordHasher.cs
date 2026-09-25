using System.Security.Cryptography;
using CommerceX.Auth.Application.Abstractions.Security;

namespace CommerceX.Auth.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;

    private const string Version = "v1";

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return string.Join(
            "$",
            Version,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        string[] parts = passwordHash.Split(
            '$',
            StringSplitOptions.None);

        if (parts.Length != 4)
        {
            return false;
        }

        if (!string.Equals(
                parts[0],
                Version,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out int iterations))
        {
            return false;
        }

        if (iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(
            actualHash,
            expectedHash);
    }
}