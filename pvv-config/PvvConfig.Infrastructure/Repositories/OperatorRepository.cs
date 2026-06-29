using Microsoft.EntityFrameworkCore;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Entities;
using PvvConfig.Infrastructure.Persistence;

namespace PvvConfig.Infrastructure.Repositories;

public class OperatorRepository(ConfigDbContext context) : IOperatorRepository
{
    public Task<Operator?> GetActiveByUsernameAsync(string username, CancellationToken ct) =>
        context.Operators.FirstOrDefaultAsync(o => o.Username == username && o.IsActive, ct);

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct) =>
        context.Operators.AnyAsync(o => o.Username == username, ct);

    public Task<List<Operator>> GetAllAsync(CancellationToken ct) =>
        context.Operators
            .Include(o => o.Company)
            .OrderBy(o => o.Username)
            .ToListAsync(ct);

    public Task<Operator?> GetByIdAsync(Guid operatorId, CancellationToken ct) =>
        context.Operators.FirstOrDefaultAsync(o => o.OperatorId == operatorId, ct);

    public async Task AddAsync(Operator op, CancellationToken ct) =>
        await context.Operators.AddAsync(op, ct);

    public Task UpdateAsync(Operator op, CancellationToken ct)
    {
        context.Operators.Update(op);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(Guid operatorId, CancellationToken ct)
    {
        var op = await context.Operators.FirstOrDefaultAsync(o => o.OperatorId == operatorId, ct);
        if (op is null)
        {
            return false;
        }
        context.Operators.Remove(op);
        return true;
    }
}
