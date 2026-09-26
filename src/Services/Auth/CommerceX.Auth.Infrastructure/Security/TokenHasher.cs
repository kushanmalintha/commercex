using System.Security.Cryptography;
using System.Text;
using CommerceX.Auth.Application.Abstractions.Security;

namespace CommerceX.Auth.Infrastructure.Security;

public sealed class TokenHasher : ITokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(token);

        byte[] hashBytes =
            SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}