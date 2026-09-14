namespace CommerceX.Auth.Application.Abstractions.Messaging;

public interface IAuthEventPublisher
{
    Task PublishUserRegisteredAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task PublishPasswordChangedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}