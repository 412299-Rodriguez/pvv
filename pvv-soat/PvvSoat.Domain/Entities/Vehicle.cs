using PvvSoat.Domain.Enums;

namespace PvvSoat.Domain.Entities;

public class Vehicle
{
    public Guid VehicleId { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public VehicleType VehicleType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
