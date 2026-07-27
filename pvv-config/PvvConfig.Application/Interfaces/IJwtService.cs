using PvvConfig.Domain.Entities;

namespace PvvConfig.Application.Interfaces;

public interface IJwtService
{
    /// <summary>
    /// Generates a signed JWT for the operator and returns it with its expiry.
    /// </summary>
    /// <param name="companyToken">
    /// The operator's HashedCompanyId, or null for a SystemAdmin. It travels as a
    /// signed claim so other services (pvv-bff) can scope a query to one company
    /// without having to resolve the hash themselves.
    /// </param>
    (string Token, DateTime ExpiresAt) GenerateToken(Operator op, string? companyToken);
}
