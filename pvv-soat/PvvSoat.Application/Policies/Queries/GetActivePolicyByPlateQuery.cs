using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Interfaces;

namespace PvvSoat.Application.Policies.Queries;

/// <summary>Returns the active/issued policy for a plate, or null if none.</summary>
public record GetActivePolicyByPlateQuery(string Plate) : IRequest<PolicyDto?>;

public class GetActivePolicyByPlateHandler(
    IVehicleRepository vehicles,
    IPolicyRepository policies) : IRequestHandler<GetActivePolicyByPlateQuery, PolicyDto?>
{
    public async Task<PolicyDto?> Handle(GetActivePolicyByPlateQuery request, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByPlateAsync(request.Plate, ct);
        if (vehicle is null)
        {
            return null;
        }

        var policy = await policies.GetActiveByVehicleAsync(vehicle.VehicleId, ct);
        return policy is null ? null : PolicyDto.FromEntity(policy);
    }
}
