using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.DTOs;

public record ConfigurationDto(
    Guid ConfigurationId,
    Guid? CompanyId,
    string ConfigurationType,
    string ConfigurationValue,
    int Version,
    DateTime UpdatedAt)
{
    public static ConfigurationDto FromEntity(Configuration configuration) => new(
        configuration.ConfigurationId,
        configuration.CompanyId,
        configuration.ConfigurationType,
        configuration.ConfigurationValue,
        configuration.Version,
        configuration.UpdatedAt);
}
