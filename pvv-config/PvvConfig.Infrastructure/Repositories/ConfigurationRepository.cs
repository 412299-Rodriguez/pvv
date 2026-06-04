using Microsoft.EntityFrameworkCore;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Entities;
using PvvConfig.Infrastructure.Persistence;

namespace PvvConfig.Infrastructure.Repositories;

public class ConfigurationRepository(ConfigDbContext context) : IConfigurationRepository
{
    public Task<Configuration?> GetActiveAsync(
        Guid? companyId, string configurationType, CancellationToken ct) =>
        context.Configurations.FirstOrDefaultAsync(
            c => c.CompanyId == companyId && c.ConfigurationType == configurationType && c.IsActive, ct);

    public async Task AddAsync(Configuration configuration, CancellationToken ct) =>
        await context.Configurations.AddAsync(configuration, ct);

    public Task UpdateAsync(Configuration configuration, CancellationToken ct)
    {
        context.Configurations.Update(configuration);
        return Task.CompletedTask;
    }

    public async Task AddHistoryAsync(ConfigurationHistory history, CancellationToken ct) =>
        await context.ConfigurationHistories.AddAsync(history, ct);
}
