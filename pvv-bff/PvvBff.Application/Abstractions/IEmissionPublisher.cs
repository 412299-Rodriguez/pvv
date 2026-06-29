using PvvBff.Application.Payments;

namespace PvvBff.Application.Abstractions;

/// <summary>Publishes an emission job to the message broker (RabbitMQ).</summary>
public interface IEmissionPublisher
{
    Task PublishAsync(EmissionMessage message, CancellationToken ct);
}
