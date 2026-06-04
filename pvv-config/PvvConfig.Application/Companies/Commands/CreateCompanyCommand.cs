using System.Text.Json;
using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.DTOs.ConfigurationValues;
using PvvConfig.Application.Exceptions;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Constants;
using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Companies.Commands;

public record CreateCompanyCommand(string Name, string CUIT) : IRequest<CompanyDto>;

public class CreateCompanyHandler(
    ICompanyRepository companies,
    IConfigurationRepository configurations,
    IEncryptionService encryption,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        if (await companies.ExistsByCuitAsync(request.CUIT, ct))
        {
            throw new ConflictException($"A company with CUIT '{request.CUIT}' already exists.");
        }

        var now = DateTime.UtcNow;
        var companyId = Guid.NewGuid();

        var company = new Company
        {
            CompanyId = companyId,
            HashedCompanyId = encryption.Encrypt(companyId.ToString()),
            Name = request.Name,
            CUIT = request.CUIT,
            IsActive = true,
            CreatedAt = now
        };

        // Seed an empty UI config with sensible defaults.
        var defaultUi = new UiConfigValueDto
        {
            PrimaryColor = "#0071ce",
            SecondaryColor = "#003d7a",
            LogoUrl = "",
            WelcomeText = "Cotizá tu seguro",
            FooterText = $"{request.Name} {now.Year}",
            CompanyDisplayName = request.Name
        };

        var uiConfig = new Configuration
        {
            ConfigurationId = Guid.NewGuid(),
            CompanyId = companyId,
            ConfigurationType = ConfigurationTypes.PVV_UI_CONFIG,
            ConfigurationValue = JsonSerializer.Serialize(defaultUi),
            IsActive = true,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            await companies.AddAsync(company, innerCt);
            await configurations.AddAsync(uiConfig, innerCt);
            await unitOfWork.SaveChangesAsync(innerCt);
        }, ct);

        return CompanyDto.FromEntity(company);
    }
}
