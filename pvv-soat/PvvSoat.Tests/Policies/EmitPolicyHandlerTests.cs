using Moq;
using PvvSoat.Application.Exceptions;
using PvvSoat.Application.Interfaces;
using PvvSoat.Application.Policies.Commands;
using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Tests.Policies;

/// <summary>
/// Emission is the one operation the buyer has already paid for, so every branch
/// here is a way to take someone's money and hand back the wrong thing: no policy,
/// a second policy, or a policy that overlaps one they already hold.
/// </summary>
public class EmitPolicyHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgets = new(MockBehavior.Strict);
    private readonly Mock<IPolicyRepository> _policies = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly EmitPolicyHandler _handler;

    public EmitPolicyHandlerTests()
    {
        // The real implementation opens a serializable transaction; here it only has
        // to run the body, so what the handler does inside it is what gets asserted.
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((operation, ct) => operation(ct));

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new EmitPolicyHandler(_budgets.Object, _policies.Object, _unitOfWork.Object);
    }

    // ---- Guard clauses: nothing is emitted -------------------------------------

    [Fact]
    public async Task Emitting_against_an_unknown_budget_is_rejected()
    {
        var budgetId = Guid.NewGuid();
        _budgets.Setup(b => b.GetByIdAsync(budgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new EmitPolicyCommand(budgetId), CancellationToken.None));

        _policies.Verify(p => p.AddAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(BudgetStatus.Converted)]
    [InlineData(BudgetStatus.Expired)]
    public async Task A_budget_that_is_not_active_cannot_be_emitted(BudgetStatus status)
    {
        var budget = ActiveBudget();
        budget.Status = status;
        GivenBudget(budget);

        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(new EmitPolicyCommand(budget.BudgetId), CancellationToken.None));

        _policies.Verify(p => p.AddAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task An_expired_budget_cannot_be_emitted()
    {
        // The price was quoted against rules that may no longer hold.
        var budget = ActiveBudget();
        budget.ValidUntil = DateTime.UtcNow.AddMinutes(-1);
        GivenBudget(budget);

        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(new EmitPolicyCommand(budget.BudgetId), CancellationToken.None));

        _policies.Verify(p => p.AddAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Idempotency -----------------------------------------------------------

    [Fact]
    public async Task A_budget_already_emitted_returns_the_same_policy_instead_of_a_second_one()
    {
        // The emission queue can redeliver: a duplicate message must not sell twice.
        var budget = ActiveBudget();
        GivenBudget(budget);

        var existing = new Policy
        {
            PolicyId = Guid.NewGuid(),
            PolicyNumber = "PVV-2026-000007",
            BudgetId = budget.BudgetId,
            VehicleId = budget.VehicleId,
            Status = PolicyStatus.Issued,
        };
        _policies.Setup(p => p.GetByBudgetIdAsync(budget.BudgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(new EmitPolicyCommand(budget.BudgetId), CancellationToken.None);

        Assert.Equal("PVV-2026-000007", result.PolicyNumber);
        _policies.Verify(p => p.AddAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---- First emission --------------------------------------------------------

    [Fact]
    public async Task A_first_policy_starts_today_and_is_numbered_for_the_current_year()
    {
        var budget = ActiveBudget();
        GivenBudget(budget);
        GivenNoExistingPolicy(budget);
        GivenNextSequence(1);

        var before = DateTime.UtcNow;
        var result = await _handler.Handle(new EmitPolicyCommand(budget.BudgetId), CancellationToken.None);

        Assert.Equal($"PVV-{DateTime.UtcNow.Year}-000001", result.PolicyNumber);
        Assert.InRange(result.StartDate, before, DateTime.UtcNow);
        Assert.Equal(result.StartDate.AddDays(365), result.EndDate);
        Assert.Equal(budget.Price, result.Price);
    }

    [Fact]
    public async Task The_sequence_is_padded_to_six_digits()
    {
        var budget = ActiveBudget();
        GivenBudget(budget);
        GivenNoExistingPolicy(budget);
        GivenNextSequence(42);

        var result = await _handler.Handle(new EmitPolicyCommand(budget.BudgetId), CancellationToken.None);

        Assert.Equal($"PVV-{DateTime.UtcNow.Year}-000042", result.PolicyNumber);
    }

    [Fact]
    public async Task Emission_converts_the_budget_and_leaves_the_policy_issued()
    {
        var budget = ActiveBudget();
        GivenBudget(budget);
        GivenNoExistingPolicy(budget);
        GivenNextSequence(1);

        var result = await _handler.Handle(new EmitPolicyCommand(budget.BudgetId), CancellationToken.None);

        Assert.Equal(PolicyStatus.Issued, result.Status);
        Assert.Equal(BudgetStatus.Converted, budget.Status);
        _budgets.Verify(b => b.UpdateAsync(budget, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Renewal ---------------------------------------------------------------

    [Fact]
    public async Task Renewing_an_insured_vehicle_future_dates_the_new_policy_to_when_the_current_one_ends()
    {
        // Two policies cannot overlap, but a renewer has paid and must get a real new
        // policy — not the old one handed back. This is the regression that matters.
        var budget = ActiveBudget();
        GivenBudget(budget);
        GivenNoExistingPolicy(budget);
        GivenNextSequence(5);

        var currentEnd = DateTime.UtcNow.AddDays(90);
        _policies.Setup(p => p.GetActiveByVehicleAsync(budget.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Policy
            {
                PolicyId = Guid.NewGuid(),
                PolicyNumber = "PVV-2026-000001",
                VehicleId = budget.VehicleId,
                EndDate = currentEnd,
                Status = PolicyStatus.Issued,
            });

        var result = await _handler.Handle(new EmitPolicyCommand(budget.BudgetId), CancellationToken.None);

        Assert.Equal(currentEnd, result.StartDate);
        Assert.Equal(currentEnd.AddDays(365), result.EndDate);
        Assert.NotEqual("PVV-2026-000001", result.PolicyNumber);
        Assert.Equal($"PVV-{DateTime.UtcNow.Year}-000005", result.PolicyNumber);
    }

    // ---- Helpers ---------------------------------------------------------------

    private static Budget ActiveBudget() => new()
    {
        BudgetId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        HolderId = Guid.NewGuid(),
        CompanyId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        Price = 15000m,
        ValidUntil = DateTime.UtcNow.AddDays(7),
        Status = BudgetStatus.Active,
    };

    private void GivenBudget(Budget budget) =>
        _budgets.Setup(b => b.GetByIdAsync(budget.BudgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(budget);

    private void GivenNoExistingPolicy(Budget budget)
    {
        _policies.Setup(p => p.GetByBudgetIdAsync(budget.BudgetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);
        _policies.Setup(p => p.GetActiveByVehicleAsync(budget.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Policy?)null);
        _policies.Setup(p => p.AddAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _budgets.Setup(b => b.UpdateAsync(budget, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void GivenNextSequence(int sequence) =>
        _policies.Setup(p => p.GetNextSequenceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequence);
}
