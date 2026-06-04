namespace PvvConfig.Application.DTOs.ConfigurationValues;

public class MaintenanceModeValueDto
{
    /// <summary>One of: Disabled | Inform | Block.</summary>
    public string Mode { get; set; } = "Disabled";

    public string? Message { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
