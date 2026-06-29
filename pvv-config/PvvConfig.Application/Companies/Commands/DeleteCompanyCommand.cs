using MediatR;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Companies.Commands;

public record DeleteCompanyCommand(Guid CompanyId) : IRequest<bool>;

public class DeleteCompanyHandler(
    ICompanyRepository companies,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteCompanyCommand, bool>
{
    public async Task<bool> Handle(DeleteCompanyCommand request, CancellationToken ct)
    {
        var deleted = await companies.DeleteAsync(request.CompanyId, ct);
        if (deleted)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        return deleted;
    }
}
