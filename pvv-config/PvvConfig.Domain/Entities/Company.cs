namespace PvvConfig.Domain.Entities;

public class Company
{
    public Guid CompanyId { get; set; }

    /// <summary>AES-256-CBC token used in the public portal URL. Never changes.</summary>
    public string HashedCompanyId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string CUIT { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Configuration> Configurations { get; set; } = new List<Configuration>();
    public ICollection<Operator> Operators { get; set; } = new List<Operator>();
}
