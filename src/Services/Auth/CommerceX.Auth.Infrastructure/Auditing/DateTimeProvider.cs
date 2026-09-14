using CommerceX.Auth.Application.Abstractions.Auditing;

namespace CommerceX.Auth.Infrastructure.Auditing;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}