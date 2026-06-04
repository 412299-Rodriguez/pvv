using PvvSoat.Domain.Enums;

namespace PvvSoat.Domain.Entities;

public class Policy
{
    public Guid PolicyId { get; set; }

    // Format: PVV-{year}-{000000}
    public string PolicyNumber { get; set; } = string.Empty;

    public Guid BudgetId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid HolderId { get; set; }

    // CompanyId and ProductId come from pvv-config (a different service), so they
    // are plain columns, not foreign keys.
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }

    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PolicyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Budget Budget { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Holder Holder { get; set; } = null!;
}
