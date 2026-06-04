using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Application.DTOs;

public record PolicyDto(
    Guid PolicyId,
    string PolicyNumber,
    Guid BudgetId,
    Guid VehicleId,
    Guid HolderId,
    Guid CompanyId,
    Guid ProductId,
    decimal Price,
    DateTime StartDate,
    DateTime EndDate,
    PolicyStatus Status)
{
    public static PolicyDto FromEntity(Policy policy) => new(
        policy.PolicyId,
        policy.PolicyNumber,
        policy.BudgetId,
        policy.VehicleId,
        policy.HolderId,
        policy.CompanyId,
        policy.ProductId,
        policy.Price,
        policy.StartDate,
        policy.EndDate,
        policy.Status);
}
