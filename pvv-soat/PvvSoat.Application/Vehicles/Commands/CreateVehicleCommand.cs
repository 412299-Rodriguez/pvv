using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Application.Vehicles.Commands;

public record CreateVehicleCommand(
    string Plate,
    string Brand,
    string Model,
    int Year,
    VehicleType VehicleType) : IRequest<VehicleDto>;

public class CreateVehicleHandler(IVehicleRepository vehicles, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    public async Task<VehicleDto> Handle(CreateVehicleCommand request, CancellationToken ct)
    {
        // Upsert by plate: if it already exists, return the existing vehicle.
        var existing = await vehicles.GetByPlateAsync(request.Plate, ct);
        if (existing is not null)
        {
            return VehicleDto.FromEntity(existing);
        }

        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Plate = request.Plate,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            VehicleType = request.VehicleType,
            CreatedAt = DateTime.UtcNow
        };

        await vehicles.AddAsync(vehicle, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return VehicleDto.FromEntity(vehicle);
    }
}
