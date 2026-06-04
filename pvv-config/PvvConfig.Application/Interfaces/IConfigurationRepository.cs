using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Interfaces;

public interface IConfigurationRepository
{
    Task<Configuration?> GetActiveAsync(Guid? companyId, string configurationType, CancellationToken ct);
    Task AddAsync(Configuration configuration, CancellationToken ct);
    Task UpdateAsync(Configuration configuration, CancellationToken ct);
    Task AddHistoryAsync(ConfigurationHistory history, CancellationToken ct);
}
