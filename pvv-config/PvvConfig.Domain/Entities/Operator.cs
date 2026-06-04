namespace PvvConfig.Domain.Entities;

public class Operator
{
    public Guid OperatorId { get; set; }
    public Guid CompanyId { get; set; }
    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}
