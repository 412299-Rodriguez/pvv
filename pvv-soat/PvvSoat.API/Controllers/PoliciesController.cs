using MediatR;
using Microsoft.AspNetCore.Mvc;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Policies.Commands;
using PvvSoat.Application.Policies.Queries;

namespace PvvSoat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController(IMediator mediator) : ControllerBase
{
    /// <summary>Emits a policy from an active, non-expired budget.</summary>
    /// <response code="201">The policy was emitted.</response>
    /// <response code="400">The budget is not active or has expired.</response>
    /// <response code="404">The budget does not exist.</response>
    /// <response code="409">A policy already exists for the budget, or for the vehicle + company.</response>
    [HttpPost("emit")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Emit([FromBody] EmitPolicyCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetByNumber), new { number = result.PolicyNumber }, result);
    }

    /// <summary>Gets the active/issued policy for a plate (for the plate search).</summary>
    /// <response code="200">An active policy exists for the plate.</response>
    /// <response code="404">No active policy for that plate.</response>
    [HttpGet("active/{plate}")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveByPlate(string plate, CancellationToken ct)
    {
        var result = await mediator.Send(new GetActivePolicyByPlateQuery(plate), ct);
        return result is null
            ? Problem(detail: $"No active policy for plate '{plate}'.", statusCode: StatusCodes.Status404NotFound)
            : Ok(result);
    }

    /// <summary>Gets a policy by its policy number.</summary>
    /// <response code="200">The policy was found.</response>
    /// <response code="404">No policy exists with that number.</response>
    [HttpGet("{number}")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string number, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPolicyByNumberQuery(number), ct);
        return result is null
            ? Problem(detail: $"Policy '{number}' was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(result);
    }
}
