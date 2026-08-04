using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Email;

/// <summary>Builds portal links from the configured public base URL.</summary>
public sealed class PortalLinkBuilder : IPortalLinkBuilder
{
    private readonly EmailOptions _options;

    public PortalLinkBuilder(IOptions<EmailOptions> options) => _options = options.Value;

    public string BuildPortalUrl(string companyToken) =>
        $"{_options.PortalBaseUrl.TrimEnd('/')}/?c={Uri.EscapeDataString(companyToken)}";
}
