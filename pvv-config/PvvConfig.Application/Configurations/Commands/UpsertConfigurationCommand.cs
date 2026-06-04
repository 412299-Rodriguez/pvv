using System.Text.Json;
using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Exceptions;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Configurations.Commands;

public record UpsertConfigurationCommand(
    Guid CompanyId,
    string ConfigurationType,
    JsonElement Value,
    string ChangedBy) : IRequest<ConfigurationDto>;

public class UpsertConfigurationHandler(
    ICompanyRepository companies,
    IConfigurationRepository configurations,
    IConfigCache cache,
    IUnitOfWork unitOfWork) : IRequestHandler<UpsertConfigurationCommand, ConfigurationDto>
{
    public async Task<ConfigurationDto> Handle(UpsertConfigurationCommand request, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(request.CompanyId, ct)
            ?? throw new NotFoundException($"Company '{request.CompanyId}' was not found.");

        var json = request.Value.GetRawText();
        var now = DateTime.UtcNow;

        var existing = await configurations.GetActiveAsync(request.CompanyId, request.ConfigurationType, ct);

        Configuration result;

        if (existing is not null)
        {
            var valueBefore = existing.ConfigurationValue;

            await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
            {
                existing.ConfigurationValue = json;
                existing.Version += 1;
                existing.UpdatedAt = now;
                await configurations.UpdateAsync(existing, innerCt);

                await configurations.AddHistoryAsync(new ConfigurationHistory
                {
                    HistoryId = Guid.NewGuid(),
                    ConfigurationId = existing.ConfigurationId,
                    CompanyId = request.CompanyId,
                    ConfigurationType = request.ConfigurationType,
                    ValueBefore = valueBefore,
                    ValueAfter = json,
                    ChangedBy = request.ChangedBy,
                    ChangedAt = now
                }, innerCt);

                await unitOfWork.SaveChangesAsync(innerCt);
            }, ct);

            result = existing;
        }
        else
        {
            result = new Configuration
            {
                ConfigurationId = Guid.NewGuid(),
                CompanyId = request.CompanyId,
                ConfigurationType = request.ConfigurationType,
                ConfigurationValue = json,
                IsActive = true,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            await configurations.AddAsync(result, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        // Invalidate the Redis cache: publish on the channel and drop the stale key
        // so the BFF falls back to HTTP until the worker repopulates it.
        await cache.PublishInvalidateAsync(company.HashedCompanyId, request.ConfigurationType, ct);
        await cache.RemoveAsync(
            IConfigCache.BuildKey(company.HashedCompanyId, request.ConfigurationType), ct);

        return ConfigurationDto.FromEntity(result);
    }
}
