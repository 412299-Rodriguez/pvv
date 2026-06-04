using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Application.DTOs;

public record VehicleDto(
    Guid VehicleId,
    string Plate,
    string Brand,
    string Model,
    int Year,
    VehicleType VehicleType)
{
    public static VehicleDto FromEntity(Vehicle vehicle) => new(
        vehicle.VehicleId,
        vehicle.Plate,
        vehicle.Brand,
        vehicle.Model,
        vehicle.Year,
        vehicle.VehicleType);
}
