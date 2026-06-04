using MediatR;
using Microsoft.AspNetCore.Mvc;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Holders.Commands;
using PvvSoat.Application.Holders.Queries;

namespace PvvSoat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HoldersController(IMediator mediator) : ControllerBase
{
    /// <summary>Gets a holder by its DNI.</summary>
    /// <response code="200">The holder was found.</response>
    /// <response code="404">No holder exists with that DNI.</response>
    [HttpGet("{dni}")]
    [ProducesResponseType(typeof(HolderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDni(string dni, CancellationToken ct)
    {
        var result = await mediator.Send(new GetHolderByDniQuery(dni), ct);
        return result is null
            ? Problem(detail: $"Holder with DNI '{dni}' was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(result);
    }

    /// <summary>Creates a holder (upsert by DNI: returns the existing one if the DNI already exists).</summary>
    /// <response code="200">The holder was created or already existed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(HolderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateHolderCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }
}
