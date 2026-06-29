namespace PvvEmission.Application.Messaging;

/// <summary>
/// Mirror of the contract pvv-bff publishes to pvv_emission_queue. Separate
/// services don't share code, so this is an independent copy of the same JSON
/// shape (field names must match what the BFF serializes).
/// </summary>
public sealed record EmissionMessage(
    string TransactionId,
    string BudgetId,
    string? CompanyToken,
    string? SessionId,
    decimal Amount,
    DateTime OccurredAt);
