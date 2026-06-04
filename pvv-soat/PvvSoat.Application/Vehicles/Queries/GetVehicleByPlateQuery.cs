using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Interfaces;

namespace PvvSoat.Application.Vehicles.Queries;

public record GetVehicleByPlateQuery(string Plate) : IRequest<VehicleDto?>;

public class GetVehicleByPlateHandler(IVehicleRepository vehicles)
    : IRequestHandler<GetVehicleByPlateQuery, VehicleDto?>
{
    public async Task<VehicleDto?> Handle(GetVehicleByPlateQuery request, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByPlateAsync(request.Plate, ct);
        return vehicle is null ? null : VehicleDto.FromEntity(vehicle);
    }
}
