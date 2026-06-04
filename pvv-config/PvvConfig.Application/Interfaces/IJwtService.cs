using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Interfaces;

public interface IJwtService
{
    /// <summary>Generates a signed JWT for the operator and returns it with its expiry.</summary>
    (string Token, DateTime ExpiresAt) GenerateToken(Operator op);
}
