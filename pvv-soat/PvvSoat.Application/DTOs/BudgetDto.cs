using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Application.DTOs;

public record BudgetDto(
    Guid BudgetId,
    Guid VehicleId,
    Guid HolderId,
    Guid CompanyId,
    Guid ProductId,
    decimal Price,
    DateTime ValidUntil,
    BudgetStatus Status)
{
    public static BudgetDto FromEntity(Budget budget) => new(
        budget.BudgetId,
        budget.VehicleId,
        budget.HolderId,
        budget.CompanyId,
        budget.ProductId,
        budget.Price,
        budget.ValidUntil,
        budget.Status);
}
