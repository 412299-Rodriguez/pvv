using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Exceptions;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Operators.Commands;

/// <summary>Updates a company operator. Password is optional (only changed if provided).</summary>
public record UpdateOperatorCommand(Guid OperatorId, Guid CompanyId, bool IsActive, string? Password)
    : IRequest<OperatorDto>;

public class UpdateOperatorHandler(
    IOperatorRepository operators,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOperatorCommand, OperatorDto>
{
    public async Task<OperatorDto> Handle(UpdateOperatorCommand request, CancellationToken ct)
    {
        var op = await operators.GetByIdAsync(request.OperatorId, ct)
            ?? throw new NotFoundException($"Operator '{request.OperatorId}' was not found.");

        op.CompanyId = request.CompanyId;
        op.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            op.PasswordHash = passwordHasher.Hash(request.Password);
        }

        await operators.UpdateAsync(op, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return OperatorDto.FromEntity(op);
    }
}
