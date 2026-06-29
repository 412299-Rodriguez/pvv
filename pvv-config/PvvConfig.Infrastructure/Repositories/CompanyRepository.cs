using Microsoft.EntityFrameworkCore;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Entities;
using PvvConfig.Infrastructure.Persistence;

namespace PvvConfig.Infrastructure.Repositories;

public class CompanyRepository(ConfigDbContext context) : ICompanyRepository
{
    public Task<List<Company>> GetAllAsync(CancellationToken ct) =>
        context.Companies.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Company?> GetByIdAsync(Guid companyId, CancellationToken ct) =>
        context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId, ct);

    public Task<Company?> GetByHashedIdAsync(string hashedCompanyId, CancellationToken ct) =>
        context.Companies.FirstOrDefaultAsync(c => c.HashedCompanyId == hashedCompanyId, ct);

    public Task<bool> ExistsByCuitAsync(string cuit, CancellationToken ct) =>
        context.Companies.AnyAsync(c => c.CUIT == cuit, ct);

    public async Task AddAsync(Company company, CancellationToken ct) =>
        await context.Companies.AddAsync(company, ct);

    public Task UpdateAsync(Company company, CancellationToken ct)
    {
        context.Companies.Update(company);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid companyId, CancellationToken ct)
    {
        var company = await context.Companies
            .Include(c => c.Operators)
            .Include(c => c.Configurations)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId, ct);
        if (company is null)
        {
            return false;
        }

        // FKs are Restrict, so remove dependents bottom-up: history → configs → operators → company.
        var configIds = company.Configurations.Select(c => c.ConfigurationId).ToList();
        var histories = await context.ConfigurationHistories
            .Where(h => h.CompanyId == companyId || configIds.Contains(h.ConfigurationId))
            .ToListAsync(ct);

        context.ConfigurationHistories.RemoveRange(histories);
        context.Configurations.RemoveRange(company.Configurations);
        context.Operators.RemoveRange(company.Operators);
        context.Companies.Remove(company);
        return true;
    }
}
