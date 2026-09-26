using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CommerceX.Auth.Domain.Enums;
using CommerceX.Auth.Infrastructure.Security;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CommerceX.Auth.UnitTests.Security;

public sealed class JwtTokenServiceTests
{
    private const string SigningKey =
        "test-signing-key-with-at-least-32-bytes-long";

    private readonly JwtTokenService _tokenService;

    public JwtTokenServiceTests()
    {
        JwtOptions options = new()
        {
            SigningKey = SigningKey,
            Issuer = "commercex-auth",
            Audience = "commercex-api",
            AccessTokenLifetimeMinutes = 15
        };

        _tokenService = new(
            Options.Create(options));
    }

    [Fact]
    public void CreateAccessToken_ShouldReturnToken()
    {
        Guid userId = Guid.NewGuid();

        string token = _tokenService.CreateAccessToken(
            userId,
            UserRole.Customer);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void CreateAccessToken_ShouldContainExpectedClaims()
    {
        Guid userId = Guid.NewGuid();

        string token = _tokenService.CreateAccessToken(
            userId,
            UserRole.Customer);

        JwtSecurityToken jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(token);

        Assert.Equal(
            userId.ToString(),
            jwt.Subject);

        Assert.Equal(
            "Customer",
            jwt.Claims.First(
                claim => claim.Type == "role").Value);

        Assert.Equal(
            "commercex-auth",
            jwt.Issuer);

        Assert.Contains(
            "commercex-api",
            jwt.Audiences);

        Assert.NotNull(
            jwt.Claims.FirstOrDefault(
                claim => claim.Type == JwtRegisteredClaimNames.Jti));

        Assert.NotNull(
            jwt.Claims.FirstOrDefault(
                claim => claim.Type == JwtRegisteredClaimNames.Iat));
    }

    [Fact]
    public void CreateAccessToken_ShouldSetExpiration()
    {
        DateTime before = DateTime.UtcNow.AddMinutes(14);

        string token = _tokenService.CreateAccessToken(
            Guid.NewGuid(),
            UserRole.Customer);

        JwtSecurityToken jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(token);

        DateTime after = DateTime.UtcNow.AddMinutes(16);

        Assert.True(jwt.ValidTo >= before);
        Assert.True(jwt.ValidTo <= after);
    }

    [Fact]
    public void CreateAccessToken_ShouldCreateDifferentJtiValues()
    {
        Guid userId = Guid.NewGuid();

        string firstToken =
            _tokenService.CreateAccessToken(
                userId,
                UserRole.Customer);

        string secondToken =
            _tokenService.CreateAccessToken(
                userId,
                UserRole.Customer);

        JwtSecurityToken firstJwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(firstToken);

        JwtSecurityToken secondJwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(secondToken);

        string firstJti =
            firstJwt.Claims.First(
                claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;

        string secondJti =
            secondJwt.Claims.First(
                claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(firstJti, secondJti);
    }

    [Fact]
    public void CreateAccessToken_ShouldSupportAdminRole()
    {
        string token =
            _tokenService.CreateAccessToken(
                Guid.NewGuid(),
                UserRole.Admin);

        JwtSecurityToken jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(token);

        Assert.Equal(
            "Admin",
            jwt.Claims.First(
                claim => claim.Type == "role").Value);
    }

    [Fact]
    public void CreateAccessToken_ShouldHaveValidSignature()
    {
        Guid userId = Guid.NewGuid();

        string token =
            _tokenService.CreateAccessToken(
                userId,
                UserRole.Customer);

        TokenValidationParameters validationParameters =
            new()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(SigningKey)),

                ValidateIssuer = true,
                ValidIssuer = "commercex-auth",

                ValidateAudience = true,
                ValidAudience = "commercex-api",

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

        JwtSecurityTokenHandler handler = new();

        ClaimsPrincipal principal =
            handler.ValidateToken(
                token,
                validationParameters,
                out _);

        Assert.Equal(
            userId.ToString(),
            principal.FindFirst(
                ClaimTypes.NameIdentifier)?.Value);
    }
}