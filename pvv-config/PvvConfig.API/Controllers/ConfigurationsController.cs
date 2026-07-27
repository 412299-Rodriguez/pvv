using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvConfig.API.Filters;
using PvvConfig.Application.Configurations.Commands;
using PvvConfig.Application.Configurations.Queries;
using PvvConfig.Application.DTOs;

namespace PvvConfig.API.Controllers;

[ApiController]
[Route("api/configurations")]
public class ConfigurationsController(IMediator mediator) : ControllerBase
{
    /// <summary>Gets a company configuration by type.</summary>
    /// <response code="200">The configuration was found.</response>
    /// <response code="404">No configuration exists for that company and type.</response>
    [HttpGet("{companyId:guid}/{type}")]
    [Authorize]
    // A tenant's own configuration: only its operator, never the platform admin.
    [CompanyOwnershipFilter(AllowSystemAdmin = false)]
    [ProducesResponseType(typeof(ConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid companyId, string type, CancellationToken ct)
    {
        var result = await mediator.Send(new GetConfigurationQuery(companyId, type), ct);
        return result is null
            ? Problem(detail: $"Configuration '{type}' for company '{companyId}' was not found.",
                statusCode: StatusCodes.Status404NotFound)
            : Ok(result);
    }

    /// <summary>Creates or updates a company configuration (free-form JSON body).</summary>
    /// <response code="200">The configuration was upserted.</response>
    /// <response code="404">The company does not exist.</response>
    [HttpPut("{companyId:guid}/{type}")]
    [Authorize]
    // A tenant's own configuration: only its operator, never the platform admin.
    [CompanyOwnershipFilter(AllowSystemAdmin = false)]
    [ProducesResponseType(typeof(ConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert(
        Guid companyId, string type, [FromBody] JsonElement value, CancellationToken ct)
    {
        var changedBy = User.FindFirst("username")?.Value ?? "system";
        var result = await mediator.Send(
            new UpsertConfigurationCommand(companyId, type, value, changedBy), ct);
        return Ok(result);
    }

    /// <summary>Internal read path for pvv-bff: returns the raw configuration JSON by hashed company id.</summary>
    /// <response code="200">The configuration JSON.</response>
    /// <response code="404">No configuration exists for that hashed company and type.</response>
    [HttpGet("internal/{hashedCompanyId}/{type}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByHash(string hashedCompanyId, string type, CancellationToken ct)
    {
        var json = await mediator.Send(new GetConfigurationByHashQuery(hashedCompanyId, type), ct);
        return json is null
            ? Problem(detail: $"Configuration '{type}' was not found.", statusCode: StatusCodes.Status404NotFound)
            : Content(json, "application/json");
    }
}
