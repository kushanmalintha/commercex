using CommerceX.Auth.Domain.Enums;

namespace CommerceX.Auth.Application.Abstractions.Security;

public interface ITokenService
{
    string CreateAccessToken(
        Guid userId,
        UserRole role);
}