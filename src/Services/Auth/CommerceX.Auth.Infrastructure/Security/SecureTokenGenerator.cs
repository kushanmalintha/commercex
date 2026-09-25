using System.Security.Cryptography;
using CommerceX.Auth.Application.Abstractions.Security;

namespace CommerceX.Auth.Infrastructure.Security;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    private const int TokenSize = 32;

    public string Generate()
    {
        byte[] tokenBytes =
            RandomNumberGenerator.GetBytes(TokenSize);

        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}