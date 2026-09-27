using System.Text.Json;
using CommerceX.Auth.Application.Abstractions.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CommerceX.Auth.Infrastructure.Messaging;

public sealed class AuthEventPublisher : IAuthEventPublisher
{
    private readonly KafkaOptions _options;
    private readonly IProducer<string, string> _producer;

    public AuthEventPublisher(
        IOptions<KafkaOptions> options)
    {
        _options = options.Value;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId
        };

        _producer = new ProducerBuilder<string, string>(
            producerConfig).Build();
    }

    public Task PublishUserRegisteredAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return PublishAsync(
            "UserRegistered",
            userId,
            cancellationToken);
    }

    public Task PublishPasswordChangedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return PublishAsync(
            "PasswordChanged",
            userId,
            cancellationToken);
    }

    private async Task PublishAsync(
        string eventType,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var eventPayload = new
        {
            eventId = Guid.NewGuid(),
            eventType,
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            producer = "auth-service",
            data = new
            {
                userId
            }
        };

        var message = new Message<string, string>
        {
            Key = userId.ToString(),
            Value = JsonSerializer.Serialize(eventPayload)
        };

        await _producer.ProduceAsync(
            _options.AuthEventsTopic,
            message,
            cancellationToken);
    }
}