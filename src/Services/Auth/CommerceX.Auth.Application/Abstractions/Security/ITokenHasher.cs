namespace CommerceX.Auth.Application.Abstractions.Security;

public interface ITokenHasher
{
    string Hash(string token);
}