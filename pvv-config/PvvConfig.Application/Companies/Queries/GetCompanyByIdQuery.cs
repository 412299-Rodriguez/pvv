using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Companies.Queries;

public record GetCompanyByIdQuery(Guid CompanyId) : IRequest<CompanyDto?>;

public class GetCompanyByIdHandler(ICompanyRepository companies)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto?>
{
    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(request.CompanyId, ct);
        return company is null ? null : CompanyDto.FromEntity(company);
    }
}
