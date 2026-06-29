using PvvSoat.Domain.Entities;

namespace PvvSoat.Application.Interfaces;

public interface IPolicyRepository
{
    Task<Policy?> GetByPolicyNumberAsync(string policyNumber, CancellationToken ct);
    Task<Policy?> GetByBudgetIdAsync(Guid budgetId, CancellationToken ct);
    Task<Policy?> GetActiveByVehicleAsync(Guid vehicleId, CancellationToken ct);
    Task<bool> HasActivePolicyForVehicleAndCompanyAsync(Guid vehicleId, Guid companyId, CancellationToken ct);
    Task AddAsync(Policy policy, CancellationToken ct);
    Task UpdateAsync(Policy policy, CancellationToken ct);

    /// <summary>
    /// Returns the next policy sequence number for the given year, taking a
    /// range lock (UPDLOCK/HOLDLOCK) so concurrent emissions cannot collide.
    /// Must be called inside a transaction.
    /// </summary>
    Task<int> GetNextSequenceAsync(int year, CancellationToken ct);
}
