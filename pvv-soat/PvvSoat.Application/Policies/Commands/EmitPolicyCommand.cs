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

        // 4. Idempotency: if a policy was already emitted for this budget, return it
        //    (handles duplicate emission messages without failing).
        var existingPolicy = await policies.GetByBudgetIdAsync(budget.BudgetId, ct);
        if (existingPolicy is not null)
        {
            return PolicyDto.FromEntity(existingPolicy);
        }

        // 5. If the vehicle already has an active policy, this purchase is a
        //    RENEWAL: the new policy is future-dated to start when the current one
        //    ends (you can't have two overlapping policies, but you can renew).
        var activePolicy = await policies.GetActiveByVehicleAsync(budget.VehicleId, ct);
        var coverageStart = activePolicy?.EndDate ?? DateTime.UtcNow;

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
                StartDate = coverageStart,
                EndDate = coverageStart.AddDays(PolicyDurationDays),
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
