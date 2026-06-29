using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PvvEmission.Application.Emission;
using PvvEmission.Application.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PvvEmission.Worker;

/// <summary>
/// Consumes the emission queue and issues policies through pvv-soat. Declares the
/// same durable queue + DLQ topology the BFF publishes to, processes one message
/// at a time, and records the outcome on the BFF's payment transaction.
/// </summary>
public sealed class EmissionWorker : BackgroundService
{
    private const string EmissionQueue = "pvv_emission_queue";
    private const string EmissionDlq = "pvv_emission_dlq";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmissionOptions _options;
    private readonly ILogger<EmissionWorker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public EmissionWorker(
        ConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<EmissionOptions> options,
        ILogger<EmissionWorker> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Same topology the BFF declares (durable queue dead-lettering to the DLQ).
        await _channel.QueueDeclareAsync(
            EmissionDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = EmissionDlq,
        };
        await _channel.QueueDeclareAsync(
            EmissionQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments, cancellationToken: stoppingToken);

        // One in-flight message at a time (a single worker, ordered processing).
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(ea, stoppingToken);
        await _channel.BasicConsumeAsync(EmissionQueue, autoAck: false, consumer, stoppingToken);

        _logger.LogInformation("EmissionWorker consuming {Queue}", EmissionQueue);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        EmissionMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<EmissionMessage>(ea.Body.Span, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Dropping poison message (invalid JSON)");
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
            return;
        }

        if (message is null)
        {
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var emitter = scope.ServiceProvider.GetRequiredService<IPolicyEmitter>();
        var store = scope.ServiceProvider.GetRequiredService<IEmissionStatusStore>();

        var delays = _options.RetryDelaysSeconds.Length > 0
            ? _options.RetryDelaysSeconds
            : EmissionOptions.DefaultRetryDelaysSeconds;
        var maxAttempts = delays.Length + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await store.MarkEmittingAsync(message.TransactionId, attempt, ct);
            var outcome = await emitter.EmitAsync(message.BudgetId, ct);

            switch (outcome.Result)
            {
                // Emitted (or already emitted — idempotent): done.
                case EmitResult.Success or EmitResult.AlreadyEmitted:
                    await store.MarkSuccessAsync(message.TransactionId, outcome.PolicyNumber, attempt, ct);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
                    _logger.LogInformation("Emitted policy {Policy} for tx {Tx} (attempt {Attempt})",
                        outcome.PolicyNumber, message.TransactionId, attempt);
                    return;

                // Bad/missing budget: retrying won't help → dead-letter.
                case EmitResult.InvalidData:
                    await store.MarkFailedAsync(message.TransactionId, "failed", attempt, ct);
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);
                    _logger.LogWarning("Emission permanently failed for tx {Tx}: {Error} → DLQ",
                        message.TransactionId, outcome.Error);
                    return;

                // Transient: back off and retry, or dead-letter once exhausted.
                case EmitResult.Transient:
                    if (attempt <= delays.Length)
                    {
                        var delay = TimeSpan.FromSeconds(delays[attempt - 1]);
                        _logger.LogWarning(
                            "Transient emit failure for tx {Tx} (attempt {Attempt}): {Error}; retrying in {Delay}s",
                            message.TransactionId, attempt, outcome.Error, delay.TotalSeconds);
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    await store.MarkFailedAsync(message.TransactionId, "retry-exhausted", attempt, ct);
                    await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct);
                    _logger.LogError("Emission retries exhausted for tx {Tx} → DLQ", message.TransactionId);
                    return;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
