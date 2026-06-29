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

    /// <summary>Terms &amp; Conditions text shown in the legal modal.</summary>
    public string TermsText { get; set; } = string.Empty;

    /// <summary>Privacy Policy text shown in the legal modal.</summary>
    public string PrivacyText { get; set; } = string.Empty;

    /// <summary>Frequently asked questions shown in the FAQ modal.</summary>
    public List<FaqItemDto> Faqs { get; set; } = new();
}

public class FaqItemDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
