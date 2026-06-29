using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Interfaces;

public interface IOperatorRepository
{
    Task<Operator?> GetActiveByUsernameAsync(string username, CancellationToken ct);
    Task<Operator?> GetByIdAsync(Guid operatorId, CancellationToken ct);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct);
    Task<List<Operator>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Operator op, CancellationToken ct);
    Task UpdateAsync(Operator op, CancellationToken ct);

    /// <summary>Deletes an operator. False if not found.</summary>
    Task<bool> DeleteAsync(Guid operatorId, CancellationToken ct);
}
