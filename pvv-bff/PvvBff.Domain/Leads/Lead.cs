namespace PvvBff.Domain.Leads;

/// <summary>
/// A single purchase attempt, built incrementally from the wizard's events
/// (materialized view of the conversion funnel). One document per flow in the
/// MongoDB collection <c>leads</c>; it is what the admin dashboard reads.
/// </summary>
public sealed class Lead
{
    /// <summary>Flow id — the Mongo _id. One per purchase attempt, minted by the front.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The company the portal belongs to (HashedCompanyId).</summary>
    public string? CompanyToken { get; set; }

    /// <summary>Anonymous session the attempt belongs to (may span several flows).</summary>
    public string? SessionId { get; set; }

    /// <summary>Furthest funnel milestone reached, 0-5. Only ever increases.</summary>
    public int LastStep { get; set; }

    /// <summary>active | abandoned | completed (see <see cref="LeadStatus"/>).</summary>
    public string Status { get; set; } = LeadStatus.Active;

    // ---- Contact details, lifted out of step 2 for abandoned-cart recovery ----
    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? AbandonedAt { get; set; }

    /// <summary>Message of the last wizard error, if any.</summary>
    public string? LastError { get; set; }

    public LeadSteps Steps { get; set; } = new();
}

/// <summary>The five funnel milestones. Each is filled by its own events.</summary>
public sealed class LeadSteps
{
    public LeadVehicleStep Step1 { get; set; } = new();

    public LeadHolderStep Step2 { get; set; } = new();

    public LeadQuoteStep Step3 { get; set; } = new();

    public LeadPaymentStep Step4 { get; set; } = new();

    public LeadEmissionStep Step5 { get; set; } = new();
}

/// <summary>Step 1 — we know which vehicle the visitor wants to insure.</summary>
public sealed class LeadVehicleStep
{
    public string? Status { get; set; }

    public string? Plate { get; set; }

    public string? VehicleTitle { get; set; }
}

/// <summary>Step 2 — we know who the policyholder is and how to reach them.</summary>
public sealed class LeadHolderStep
{
    public string? Status { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Dni { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }
}

/// <summary>Step 3 — a product and a price were chosen.</summary>
public sealed class LeadQuoteStep
{
    public string? Status { get; set; }

    public int? OptionsCount { get; set; }

    public string? ProductId { get; set; }

    public string? ProductName { get; set; }

    public decimal? Amount { get; set; }
}

/// <summary>Step 4 — the payment (initiated / completed / rejected / abandoned).</summary>
public sealed class LeadPaymentStep
{
    public string? Status { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TransactionId { get; set; }

    public string? PreferenceId { get; set; }
}

/// <summary>Step 5 — the policy was emitted.</summary>
public sealed class LeadEmissionStep
{
    public string? Status { get; set; }

    public string? PolicyNumber { get; set; }
}

/// <summary>Lead-level status values.</summary>
public static class LeadStatus
{
    public const string Active = "active";
    public const string Abandoned = "abandoned";
    public const string Completed = "completed";
}
