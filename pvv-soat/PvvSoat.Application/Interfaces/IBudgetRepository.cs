using PvvSoat.Domain.Entities;

namespace PvvSoat.Application.Interfaces;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(Guid budgetId, CancellationToken ct);
    Task AddAsync(Budget budget, CancellationToken ct);
    Task UpdateAsync(Budget budget, CancellationToken ct);
}
