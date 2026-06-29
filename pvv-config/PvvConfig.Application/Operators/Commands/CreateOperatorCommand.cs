using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Exceptions;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Entities;
using PvvConfig.Domain.Enums;

namespace PvvConfig.Application.Operators.Commands;

public record CreateOperatorCommand(string Username, string Password, Guid CompanyId)
    : IRequest<OperatorDto>;

public class CreateOperatorHandler(
    IOperatorRepository operators,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateOperatorCommand, OperatorDto>
{
    public async Task<OperatorDto> Handle(CreateOperatorCommand request, CancellationToken ct)
    {
        if (await operators.ExistsByUsernameAsync(request.Username, ct))
        {
            throw new ConflictException($"An operator with username '{request.Username}' already exists.");
        }

        var op = new Operator
        {
            OperatorId = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Username = request.Username,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = OperatorRole.CompanyOperator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await operators.AddAsync(op, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return OperatorDto.FromEntity(op);
    }
}
