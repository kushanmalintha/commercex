using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommerceX.Auth.Application.Abstractions.Security;
using CommerceX.Auth.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CommerceX.Auth.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(
        IOptions<JwtOptions> options)
    {
        _options = options.Value;

        ValidateOptions();
    }

    public string CreateAccessToken(
        Guid userId,
        UserRole role)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        DateTimeOffset expiresAt =
            now.AddMinutes(
                _options.AccessTokenLifetimeMinutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("role", role.ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        ];

        SymmetricSecurityKey signingKey =
            new(Encoding.UTF8.GetBytes(_options.SigningKey));

        SigningCredentials signingCredentials =
            new(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token =
            new(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured.");
        }

        if (Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }

        if (string.IsNullOrWhiteSpace(_options.Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Audience))
        {
            throw new InvalidOperationException(
                "JWT audience is not configured.");
        }

        if (_options.AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT access-token lifetime must be greater than zero.");
        }
    }
}