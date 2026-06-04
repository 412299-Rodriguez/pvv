namespace PvvConfig.Application.DTOs.ConfigurationValues;

public class PricingItemDto
{
    public Guid PricingId { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>One of: Car | Motorcycle | Truck | Van.</summary>
    public string VehicleType { get; set; } = string.Empty;

    public int YearFrom { get; set; }
    public int YearTo { get; set; }
    public decimal Price { get; set; }
}
