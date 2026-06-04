using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.DTOs;

public record CompanyDto(
    Guid CompanyId,
    string HashedCompanyId,
    string Name,
    string CUIT,
    bool IsActive,
    DateTime CreatedAt)
{
    public static CompanyDto FromEntity(Company company) => new(
        company.CompanyId,
        company.HashedCompanyId,
        company.Name,
        company.CUIT,
        company.IsActive,
        company.CreatedAt);
}
