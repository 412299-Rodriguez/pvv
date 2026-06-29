namespace PvvEmission.Application.Emission;

/// <summary>
/// Records the emission outcome back onto the BFF's payment transaction (shared
/// MongoDB), so the front can poll EMISSION_STATUS and show the ticket.
/// </summary>
public interface IEmissionStatusStore
{
    Task MarkEmittingAsync(string transactionId, int attempt, CancellationToken ct);

    Task MarkSuccessAsync(string transactionId, string? policyNumber, int attempt, CancellationToken ct);

    Task MarkFailedAsync(string transactionId, string status, int attempt, CancellationToken ct);
}
