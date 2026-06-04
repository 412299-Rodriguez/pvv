using MediatR;
using Microsoft.AspNetCore.Mvc;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Vehicles.Commands;
using PvvSoat.Application.Vehicles.Queries;

namespace PvvSoat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController(IMediator mediator) : ControllerBase
{
    /// <summary>Gets a vehicle by its plate.</summary>
    /// <response code="200">The vehicle was found.</response>
    /// <response code="404">No vehicle exists with that plate.</response>
    [HttpGet("{plate}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPlate(string plate, CancellationToken ct)
    {
        var result = await mediator.Send(new GetVehicleByPlateQuery(plate), ct);
        return result is null
            ? Problem(detail: $"Vehicle with plate '{plate}' was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(result);
    }

    /// <summary>Creates a vehicle (upsert by plate: returns the existing one if the plate already exists).</summary>
    /// <response code="200">The vehicle was created or already existed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }
}
