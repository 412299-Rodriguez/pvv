using Microsoft.EntityFrameworkCore;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;
using PvvSoat.Infrastructure.Persistence;

namespace PvvSoat.Infrastructure.Repositories;

public class PolicyRepository(SoatDbContext context) : IPolicyRepository
{
    public Task<Policy?> GetByPolicyNumberAsync(string policyNumber, CancellationToken ct) =>
        context.Policies.FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);

    public Task<Policy?> GetByBudgetIdAsync(Guid budgetId, CancellationToken ct) =>
        context.Policies.FirstOrDefaultAsync(p => p.BudgetId == budgetId, ct);

    public Task<bool> HasActivePolicyForVehicleAndCompanyAsync(
        Guid vehicleId, Guid companyId, CancellationToken ct) =>
        context.Policies.AnyAsync(
            p => p.VehicleId == vehicleId
                 && p.CompanyId == companyId
                 && (p.Status == PolicyStatus.Active || p.Status == PolicyStatus.Issued),
            ct);

    public async Task AddAsync(Policy policy, CancellationToken ct) =>
        await context.Policies.AddAsync(policy, ct);

    public Task UpdateAsync(Policy policy, CancellationToken ct)
    {
        context.Policies.Update(policy);
        return Task.CompletedTask;
    }

    public async Task<int> GetNextSequenceAsync(int year, CancellationToken ct)
    {
        // Range lock (UPDLOCK, HOLDLOCK) so concurrent emissions for the same
        // year serialize and cannot produce duplicate policy numbers.
        var pattern = $"PVV-{year}-%";
        var count = await context.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM [Policies] WITH (UPDLOCK, HOLDLOCK) WHERE [PolicyNumber] LIKE {0}",
                pattern)
            .SingleAsync(ct);

        return count + 1;
    }
}
