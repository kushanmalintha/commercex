namespace CommerceX.Auth.Application.Abstractions.Persistence;

public interface IAuthUnitOfWork
{
    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}