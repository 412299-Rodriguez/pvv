namespace PvvBff.Application.Abstractions;

/// <summary>
/// Builds the public link back to a company's portal.
///
/// An abstraction rather than a configuration value read in the handler: where the
/// portal is deployed is infrastructure's business, and in development it is a tunnel
/// hostname that changes on every run. The handler only needs to know that a link
/// exists for a tenant.
/// </summary>
public interface IPortalLinkBuilder
{
    /// <summary>
    /// Entry point of <paramref name="companyToken"/>'s portal. The token IS the
    /// tenant's identity in the URL — there is no table of portal links.
    /// </summary>
    string BuildPortalUrl(string companyToken);
}
