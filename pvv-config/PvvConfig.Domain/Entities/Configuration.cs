namespace PvvConfig.Domain.Entities;

public class Configuration
{
    public Guid ConfigurationId { get; set; }

    /// <summary>Owning company. Null means a global/system configuration.</summary>
    public Guid? CompanyId { get; set; }

    public string ConfigurationType { get; set; } = string.Empty;

    /// <summary>Free-form JSON blob (nvarchar(max)).</summary>
    public string ConfigurationValue { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
    public ICollection<ConfigurationHistory> Histories { get; set; } = new List<ConfigurationHistory>();
}
