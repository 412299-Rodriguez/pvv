using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Exceptions;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Auth;

public record LoginCommand(string Username, string Password) : IRequest<LoginResponseDto>;

public class LoginHandler(
    IOperatorRepository operators,
    ICompanyRepository companies,
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

        // A company operator also carries its portal hash in the token, so pvv-bff
        // can scope its leads without resolving the hash on every request.
        string? companyToken = null;
        if (op.CompanyId.HasValue)
        {
            var company = await companies.GetByIdAsync(op.CompanyId.Value, ct);
            companyToken = company?.HashedCompanyId;
        }

        var (token, expiresAt) = jwtService.GenerateToken(op, companyToken);
        return new LoginResponseDto(token, expiresAt, op.CompanyId, op.Role.ToString());
    }
}
