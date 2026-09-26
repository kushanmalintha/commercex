using CommerceX.Auth.Application.Abstractions.Persistence;

namespace CommerceX.Auth.Infrastructure.Persistence;

public sealed class AuthUnitOfWork : IAuthUnitOfWork
{
    private readonly AuthDbContext _dbContext;

    public AuthUnitOfWork(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}