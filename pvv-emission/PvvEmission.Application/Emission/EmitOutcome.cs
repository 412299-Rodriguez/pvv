namespace PvvEmission.Application.Emission;

/// <summary>Classification of a soat emit attempt, which drives retry vs DLQ.</summary>
public enum EmitResult
{
    /// <summary>Policy emitted.</summary>
    Success,

    /// <summary>soat 409 — already emitted; idempotent, treat as success.</summary>
    AlreadyEmitted,

    /// <summary>soat 400/404 — bad/missing budget; do NOT retry, dead-letter.</summary>
    InvalidData,

    /// <summary>soat down / 5xx / timeout — retry with backoff.</summary>
    Transient,
}

public sealed record EmitOutcome(EmitResult Result, string? PolicyNumber, string? Error)
{
    public static EmitOutcome Success(string policyNumber) => new(EmitResult.Success, policyNumber, null);
    public static EmitOutcome AlreadyEmitted() => new(EmitResult.AlreadyEmitted, null, null);
    public static EmitOutcome InvalidData(string error) => new(EmitResult.InvalidData, null, error);
    public static EmitOutcome Transient(string error) => new(EmitResult.Transient, null, error);
}
