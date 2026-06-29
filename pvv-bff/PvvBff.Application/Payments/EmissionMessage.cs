namespace PvvBff.Application.Payments;

/// <summary>
/// Message published to RabbitMQ (pvv_emission_queue) once a payment is
/// confirmed. pvv-emission (HU-09) consumes it and emits the policy in soat
/// from the BudgetId. This is the stable contract between the two services.
/// </summary>
public sealed record EmissionMessage(
    string TransactionId,
    string BudgetId,
    string? CompanyToken,
    string? SessionId,
    decimal Amount,
    DateTime OccurredAt);
