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
}
