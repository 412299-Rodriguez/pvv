using Microsoft.EntityFrameworkCore;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;
using PvvSoat.Infrastructure.Persistence;

namespace PvvSoat.Infrastructure.Repositories;

public class BudgetRepository(SoatDbContext context) : IBudgetRepository
{
    public Task<Budget?> GetByIdAsync(Guid budgetId, CancellationToken ct) =>
        context.Budgets.FirstOrDefaultAsync(b => b.BudgetId == budgetId, ct);

    public async Task AddAsync(Budget budget, CancellationToken ct) =>
        await context.Budgets.AddAsync(budget, ct);

    public Task UpdateAsync(Budget budget, CancellationToken ct)
    {
        context.Budgets.Update(budget);
        return Task.CompletedTask;
    }
}
