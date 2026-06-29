using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Payments;
using RabbitMQ.Client;

namespace PvvBff.Infrastructure.Messaging;

/// <summary>
/// Publishes emission jobs to RabbitMQ using the async client (7.x). Holds one
/// shared connection (lazy) and opens a channel per publish. Declares the durable
/// emission queue with a dead-letter route to the DLQ, so rejected/expired
/// messages land there. The consumer (pvv-emission, HU-09) declares the same.
/// </summary>
public sealed class RabbitMqEmissionPublisher : IEmissionPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEmissionPublisher> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqEmissionPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEmissionPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(EmissionMessage message, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await DeclareTopologyAsync(channel, ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.EmissionQueue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation("Published emission message for tx {TransactionId} to {Queue}",
            message.TransactionId, _options.EmissionQueue);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                VirtualHost = _options.VHost,
                UserName = _options.User,
                Password = _options.Pass,
            };
            _connection = await factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.QueueDeclareAsync(
            _options.EmissionDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = _options.EmissionDlq,
        };
        await channel.QueueDeclareAsync(
            _options.EmissionQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments, cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}
