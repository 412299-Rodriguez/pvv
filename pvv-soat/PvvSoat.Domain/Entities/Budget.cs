using PvvSoat.Domain.Enums;

namespace PvvSoat.Domain.Entities;

public class Budget
{
    public Guid BudgetId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid HolderId { get; set; }

    // CompanyId and ProductId come from pvv-config (a different service), so they
    // are plain columns, not foreign keys.
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }

    public decimal Price { get; set; }
    public DateTime ValidUntil { get; set; }
    public BudgetStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Policy? Policy { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Holder Holder { get; set; } = null!;
}
