using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.DTOs;

public record OperatorDto(
    Guid OperatorId,
    string Username,
    Guid? CompanyId,
    string? CompanyName,
    string Role,
    bool IsActive,
    DateTime CreatedAt)
{
    public static OperatorDto FromEntity(Operator op) => new(
        op.OperatorId,
        op.Username,
        op.CompanyId,
        op.Company?.Name,
        op.Role.ToString(),
        op.IsActive,
        op.CreatedAt);
}
