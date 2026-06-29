using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Interfaces;

public interface ICompanyRepository
{
    Task<List<Company>> GetAllAsync(CancellationToken ct);
    Task<Company?> GetByIdAsync(Guid companyId, CancellationToken ct);
    Task<Company?> GetByHashedIdAsync(string hashedCompanyId, CancellationToken ct);
    Task<bool> ExistsByCuitAsync(string cuit, CancellationToken ct);
    Task AddAsync(Company company, CancellationToken ct);
    Task UpdateAsync(Company company, CancellationToken ct);

    /// <summary>Deletes a company and its configurations, history and operators. False if not found.</summary>
    Task<bool> DeleteAsync(Guid companyId, CancellationToken ct);
}
