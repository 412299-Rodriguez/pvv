using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Exceptions;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Companies.Commands;

public record UpdateCompanyCommand(Guid CompanyId, string Name, string CUIT, bool IsActive)
    : IRequest<CompanyDto>;

public class UpdateCompanyHandler(
    ICompanyRepository companies,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(request.CompanyId, ct)
            ?? throw new NotFoundException($"Company '{request.CompanyId}' was not found.");

        // HashedCompanyId never changes; only the editable fields are updated.
        company.Name = request.Name;
        company.CUIT = request.CUIT;
        company.IsActive = request.IsActive;

        await companies.UpdateAsync(company, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return CompanyDto.FromEntity(company);
    }
}
