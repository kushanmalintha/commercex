using CommerceX.Auth.Infrastructure.Security;

namespace CommerceX.Auth.UnitTests.Security;

public sealed class TokenHasherTests
{
    [Fact]
    public void Hash_ShouldReturnDeterministicHash()
    {
        // Arrange
        TokenHasher hasher = new();

        // Act
        string firstHash =
            hasher.Hash("test-refresh-token");

        string secondHash =
            hasher.Hash("test-refresh-token");

        // Assert
        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHash_WhenTokenChanges()
    {
        // Arrange
        TokenHasher hasher = new();

        // Act
        string firstHash =
            hasher.Hash("token-one");

        string secondHash =
            hasher.Hash("token-two");

        // Assert
        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void Hash_ShouldReturnExpectedSha256Hash()
    {
        // Arrange
        TokenHasher hasher = new();

        // Act
        string hash =
            hasher.Hash("test");

        // Assert
        Assert.Equal(
            "9F86D081884C7D659A2FEAA0C55AD015"
            + "A3BF4F1B2B0B822CD15D6C15B0F00A08",
            hash);
    }
}