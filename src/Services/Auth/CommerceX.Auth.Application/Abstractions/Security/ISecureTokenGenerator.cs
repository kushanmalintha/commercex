namespace CommerceX.Auth.Application.Abstractions.Security;

public interface ISecureTokenGenerator
{
    string Generate();
}