using CommerceX.Auth.Infrastructure.Security;

namespace CommerceX.Auth.UnitTests.Security;

public sealed class SecureTokenGeneratorTests
{
    private readonly SecureTokenGenerator _tokenGenerator = new();

    [Fact]
    public void Generate_ShouldReturnNonEmptyToken()
    {
        string token = _tokenGenerator.Generate();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generate_ShouldReturnDifferentTokens()
    {
        string firstToken = _tokenGenerator.Generate();
        string secondToken = _tokenGenerator.Generate();

        Assert.NotEqual(firstToken, secondToken);
    }

    [Fact]
    public void Generate_ShouldReturnUrlSafeToken()
    {
        string token = _tokenGenerator.Generate();

        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }
}