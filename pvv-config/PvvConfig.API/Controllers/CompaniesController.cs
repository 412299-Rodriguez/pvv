using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvConfig.Application.Companies.Commands;
using PvvConfig.Application.Companies.Queries;
using PvvConfig.Application.DTOs;
using PvvConfig.Domain.Enums;

namespace PvvConfig.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists all companies.</summary>
    /// <response code="200">The list of companies.</response>
    [HttpGet]
    [Authorize(Roles = nameof(OperatorRole.SystemAdmin))]
    [ProducesResponseType(typeof(List<CompanyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCompaniesQuery(), ct);
        return Ok(result);
    }

    /// <summary>Gets a company by its id.</summary>
    /// <response code="200">The company was found.</response>
    /// <response code="404">No company exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCompanyByIdQuery(id), ct);
        return result is null
            ? Problem(detail: $"Company '{id}' was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(result);
    }

    /// <summary>Creates a company and seeds its default UI configuration.</summary>
    /// <response code="201">The company was created.</response>
    /// <response code="409">A company with the same CUIT already exists.</response>
    [HttpPost]
    [Authorize(Roles = nameof(OperatorRole.SystemAdmin))]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.CompanyId }, result);
    }

    /// <summary>Updates a company's editable fields (Name, CUIT, IsActive).</summary>
    /// <response code="200">The company was updated.</response>
    /// <response code="404">No company exists with that id.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(OperatorRole.SystemAdmin))]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateCompanyCommand(id, request.Name, request.CUIT, request.IsActive), ct);
        return Ok(result);
    }

    /// <summary>Deletes a company and all its configurations and operators.</summary>
    /// <response code="204">The company was deleted.</response>
    /// <response code="404">No company exists with that id.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(OperatorRole.SystemAdmin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await mediator.Send(new DeleteCompanyCommand(id), ct);
        return deleted
            ? NoContent()
            : Problem(detail: $"Company '{id}' was not found.", statusCode: StatusCodes.Status404NotFound);
    }
}

public record UpdateCompanyRequest(string Name, string CUIT, bool IsActive);
