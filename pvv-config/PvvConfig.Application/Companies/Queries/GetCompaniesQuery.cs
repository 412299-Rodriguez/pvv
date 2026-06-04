using MediatR;
using PvvConfig.Application.DTOs;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Companies.Queries;

public record GetCompaniesQuery : IRequest<List<CompanyDto>>;

public class GetCompaniesHandler(ICompanyRepository companies)
    : IRequestHandler<GetCompaniesQuery, List<CompanyDto>>
{
    public async Task<List<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken ct)
    {
        var entities = await companies.GetAllAsync(ct);
        return entities.Select(CompanyDto.FromEntity).ToList();
    }
}
