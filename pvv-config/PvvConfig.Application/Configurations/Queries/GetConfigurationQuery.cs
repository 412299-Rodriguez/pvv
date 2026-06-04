using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Configurations.Queries;

public record GetConfigurationQuery(Guid CompanyId, string ConfigurationType)
    : IRequest<ConfigurationDto?>;

public class GetConfigurationHandler(IConfigurationRepository configurations)
    : IRequestHandler<GetConfigurationQuery, ConfigurationDto?>
{
    public async Task<ConfigurationDto?> Handle(GetConfigurationQuery request, CancellationToken ct)
    {
        var configuration = await configurations.GetActiveAsync(
            request.CompanyId, request.ConfigurationType, ct);
        return configuration is null ? null : ConfigurationDto.FromEntity(configuration);
    }
}
