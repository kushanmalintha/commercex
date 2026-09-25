using CommerceX.Auth.Infrastructure.Security;

namespace CommerceX.Auth.UnitTests.Security;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyHash()
    {
        const string password = "CorrectPassword123!";

        string hash = _passwordHasher.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForCorrectPassword()
    {
        const string password = "CorrectPassword123!";

        string hash = _passwordHasher.Hash(password);

        bool result = _passwordHasher.Verify(
            password,
            hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForIncorrectPassword()
    {
        const string password = "CorrectPassword123!";
        const string incorrectPassword = "WrongPassword123!";

        string hash = _passwordHasher.Hash(password);

        bool result = _passwordHasher.Verify(
            incorrectPassword,
            hash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_ShouldProduceDifferentHashes_ForSamePassword()
    {
        const string password = "CorrectPassword123!";

        string firstHash = _passwordHasher.Hash(password);
        string secondHash = _passwordHasher.Hash(password);

        Assert.NotEqual(firstHash, secondHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("v1$invalid")]
    [InlineData("v1$600000$invalid$invalid")]
    public void Verify_ShouldReturnFalse_ForInvalidHash(
        string passwordHash)
    {
        bool result = _passwordHasher.Verify(
            "CorrectPassword123!",
            passwordHash);

        Assert.False(result);
    }
}