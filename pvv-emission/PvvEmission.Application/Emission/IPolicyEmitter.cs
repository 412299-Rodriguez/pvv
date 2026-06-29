namespace PvvEmission.Application.Emission;

/// <summary>Emits a policy in pvv-soat from a budget id.</summary>
public interface IPolicyEmitter
{
    Task<EmitOutcome> EmitAsync(string budgetId, CancellationToken ct);
}
