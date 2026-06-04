using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvConfig.Application.Auth;
using PvvConfig.Application.DTOs;

namespace PvvConfig.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Authenticates an operator and returns a JWT.</summary>
    /// <response code="200">Authentication succeeded.</response>
    /// <response code="401">Invalid username or password.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(request.Username, request.Password), ct);
        return Ok(result);
    }
}
