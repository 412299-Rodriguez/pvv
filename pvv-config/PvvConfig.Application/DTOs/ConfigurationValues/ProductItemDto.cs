namespace PvvConfig.Application.DTOs.ConfigurationValues;

public class ProductItemDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CoverageType { get; set; } = string.Empty;
    public string Conditions { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
