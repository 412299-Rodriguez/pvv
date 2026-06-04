namespace PvvConfig.Domain.Entities;

public class ConfigurationHistory
{
    public Guid HistoryId { get; set; }
    public Guid ConfigurationId { get; set; }

    /// <summary>Snapshot of the company id at the time of the change.</summary>
    public Guid? CompanyId { get; set; }

    public string ConfigurationType { get; set; } = string.Empty;
    public string ValueBefore { get; set; } = string.Empty;
    public string ValueAfter { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Configuration Configuration { get; set; } = null!;
}
