using MediatR;
using Microsoft.AspNetCore.Mvc;
using PvvSoat.Application.Budgets.Commands;
using PvvSoat.Application.Budgets.Queries;
using PvvSoat.Application.DTOs;

namespace PvvSoat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetsController(IMediator mediator) : ControllerBase
{
    /// <summary>Creates a budget for a vehicle + holder, valid for 30 minutes.</summary>
    /// <response code="201">The budget was created.</response>
    /// <response code="404">The referenced vehicle or holder does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.BudgetId }, result);
    }

    /// <summary>Gets a budget by its id.</summary>
    /// <response code="200">The budget was found.</response>
    /// <response code="404">No budget exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetBudgetByIdQuery(id), ct);
        return result is null
            ? Problem(detail: $"Budget '{id}' was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(result);
    }
}
