namespace PvvConfig.Application.DTOs.ConfigurationValues;

public class UiConfigValueDto
{
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string CompanyDisplayName { get; set; } = string.Empty;
    public string WelcomeText { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;

    /// <summary>Per-screen copy the portal can override, keyed by a stable id.</summary>
    public Dictionary<string, string> Texts { get; set; } = new();

    /// <summary>Public image URLs shown in the step-1 advertising carousel.</summary>
    public List<string> AdImages { get; set; } = new();
}
