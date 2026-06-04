using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Interfaces;

namespace PvvSoat.Application.Budgets.Queries;

public record GetBudgetByIdQuery(Guid BudgetId) : IRequest<BudgetDto?>;

public class GetBudgetByIdHandler(IBudgetRepository budgets)
    : IRequestHandler<GetBudgetByIdQuery, BudgetDto?>
{
    public async Task<BudgetDto?> Handle(GetBudgetByIdQuery request, CancellationToken ct)
    {
        var budget = await budgets.GetByIdAsync(request.BudgetId, ct);
        return budget is null ? null : BudgetDto.FromEntity(budget);
    }
}
