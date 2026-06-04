using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Interfaces;

public interface IOperatorRepository
{
    Task<Operator?> GetActiveByUsernameAsync(string username, CancellationToken ct);
    Task AddAsync(Operator op, CancellationToken ct);
}
