namespace PvvEmission.Worker;

/// <summary>
/// Background service that consumes the RabbitMQ emission queue and issues
/// policies through pvv-soat. Skeleton only — the consumer lands in Sprint 1.
/// </summary>
public class EmissionWorker(ILogger<EmissionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmissionWorker started");

        // TODO Sprint 1: conectar RabbitMQ consumer (cola pvv_emission_queue),
        //                procesar mensajes, emitir póliza en pvv-soat, manejar
        //                reintentos con backoff exponencial y dead-letter queue.

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
