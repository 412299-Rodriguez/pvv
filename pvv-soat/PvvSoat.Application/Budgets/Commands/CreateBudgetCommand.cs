using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Exceptions;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Application.Budgets.Commands;

public record CreateBudgetCommand(
    Guid VehicleId,
    Guid HolderId,
    Guid CompanyId,
    Guid ProductId,
    decimal Price) : IRequest<BudgetDto>;

public class CreateBudgetHandler(
    IVehicleRepository vehicles,
    IHolderRepository holders,
    IBudgetRepository budgets,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBudgetCommand, BudgetDto>
{
    private const int BudgetValidityMinutes = 30;

    public async Task<BudgetDto> Handle(CreateBudgetCommand request, CancellationToken ct)
    {
        if (!await vehicles.ExistsByIdAsync(request.VehicleId, ct))
        {
            throw new NotFoundException($"Vehicle '{request.VehicleId}' was not found.");
        }

        if (!await holders.ExistsByIdAsync(request.HolderId, ct))
        {
            throw new NotFoundException($"Holder '{request.HolderId}' was not found.");
        }

        var budget = new Budget
        {
            BudgetId = Guid.NewGuid(),
            VehicleId = request.VehicleId,
            HolderId = request.HolderId,
            CompanyId = request.CompanyId,
            ProductId = request.ProductId,
            Price = request.Price,
            ValidUntil = DateTime.UtcNow.AddMinutes(BudgetValidityMinutes),
            Status = BudgetStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await budgets.AddAsync(budget, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return BudgetDto.FromEntity(budget);
    }
}
