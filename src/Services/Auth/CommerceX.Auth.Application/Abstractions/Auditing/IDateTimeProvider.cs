namespace CommerceX.Auth.Application.Abstractions.Auditing;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}