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
}
