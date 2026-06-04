using MediatR;
using PvvSoat.Application.DTOs;
using PvvSoat.Application.Exceptions;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Application.Policies.Commands;

public record EmitPolicyCommand(Guid BudgetId) : IRequest<PolicyDto>;

public class EmitPolicyHandler(
    IBudgetRepository budgets,
    IPolicyRepository policies,
    IUnitOfWork unitOfWork) : IRequestHandler<EmitPolicyCommand, PolicyDto>
{
    private const int PolicyDurationDays = 365;

    public async Task<PolicyDto> Handle(EmitPolicyCommand request, CancellationToken ct)
    {
        // 1. The budget must exist.
        var budget = await budgets.GetByIdAsync(request.BudgetId, ct)
            ?? throw new NotFoundException($"Budget '{request.BudgetId}' was not found.");

        // 2. The budget must be Active.
        if (budget.Status != BudgetStatus.Active)
        {
            throw new ValidationException(
                $"Budget '{budget.BudgetId}' is not active (current status: {budget.Status}).");
        }

        // 3. The budget must not be expired.
        if (budget.ValidUntil <= DateTime.UtcNow)
        {
            throw new ValidationException($"Budget '{budget.BudgetId}' has expired.");
        }

        // 4. There must be no policy already emitted for this budget.
        var existingPolicy = await policies.GetByBudgetIdAsync(budget.BudgetId, ct);
        if (existingPolicy is not null)
        {
            throw new ConflictException(
                $"A policy already exists for budget '{budget.BudgetId}'.");
        }

        // 5. There must be no active/issued policy for the same vehicle + company.
        if (await policies.HasActivePolicyForVehicleAndCompanyAsync(budget.VehicleId, budget.CompanyId, ct))
        {
            throw new ConflictException(
                "An active policy already exists for this vehicle and company.");
        }

        Policy policy = null!;

        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            // Generate the next policy number (range-locked against concurrent emissions).
            var year = DateTime.UtcNow.Year;
            var sequence = await policies.GetNextSequenceAsync(year, innerCt);
            var policyNumber = $"PVV-{year}-{sequence:D6}";

            var now = DateTime.UtcNow;
            policy = new Policy
            {
                PolicyId = Guid.NewGuid(),
                PolicyNumber = policyNumber,
                BudgetId = budget.BudgetId,
                VehicleId = budget.VehicleId,
                HolderId = budget.HolderId,
                CompanyId = budget.CompanyId,
                ProductId = budget.ProductId,
                Price = budget.Price,
                StartDate = now,
                EndDate = now.AddDays(PolicyDurationDays),
                Status = PolicyStatus.Pending,
                CreatedAt = now
            };

            await policies.AddAsync(policy, innerCt);

            // Convert the budget.
            budget.Status = BudgetStatus.Converted;
            await budgets.UpdateAsync(budget, innerCt);

            // Persist policy (Pending) + budget (Converted) atomically.
            await unitOfWork.SaveChangesAsync(innerCt);

            // Mark the policy as issued.
            policy.Status = PolicyStatus.Issued;
            await unitOfWork.SaveChangesAsync(innerCt);
        }, ct);

        return PolicyDto.FromEntity(policy);
    }
}
