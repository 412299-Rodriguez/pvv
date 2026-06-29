using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Operators.Commands;
using PvvConfig.Application.Operators.Queries;
using PvvConfig.Domain.Enums;

namespace PvvConfig.API.Controllers;

[ApiController]
[Route("api/operators")]
[Authorize(Roles = nameof(OperatorRole.SystemAdmin))]
public class OperatorsController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all operators (SystemAdmin only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OperatorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetOperatorsQuery(), ct));
    }

    /// <summary>Creates a company operator.</summary>
    /// <response code="200">The operator was created.</response>
    /// <response code="409">An operator with the same username already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(OperatorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateOperatorCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>Updates an operator (company, active flag, optional new password).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OperatorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOperatorRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateOperatorCommand(id, request.CompanyId, request.IsActive, request.Password), ct);
        return Ok(result);
    }

    /// <summary>Deletes an operator.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await mediator.Send(new DeleteOperatorCommand(id), ct);
        return deleted
            ? NoContent()
            : Problem(detail: $"Operator '{id}' was not found.", statusCode: StatusCodes.Status404NotFound);
    }
}

public record UpdateOperatorRequest(Guid CompanyId, bool IsActive, string? Password);
