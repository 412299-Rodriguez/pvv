using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Exceptions;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Auth;

public record LoginCommand(string Username, string Password) : IRequest<LoginResponseDto>;

public class LoginHandler(
    IOperatorRepository operators,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var op = await operators.GetActiveByUsernameAsync(request.Username, ct);
        if (op is null || !passwordHasher.Verify(request.Password, op.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        var (token, expiresAt) = jwtService.GenerateToken(op);
        return new LoginResponseDto(token, expiresAt, op.CompanyId);
    }
}
