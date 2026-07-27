namespace PvvBff.Application.Leads.Queries;

/// <summary>
/// The slice of the funnel a dashboard request is asking about.
/// </summary>
/// <param name="CompanyToken">
/// Scopes the query to one tenant. Resolved from the caller's signed token, never
/// from user input — except for a SystemAdmin, who may target any company (or
/// pass null to see them all).
/// </param>
/// <param name="LastStep">Keep only leads that stopped exactly at this milestone.</param>
/// <param name="Status">active | abandoned | completed.</param>
public sealed record LeadQueryFilter(
    string? CompanyToken = null,
    DateTime? From = null,
    DateTime? To = null,
    int? LastStep = null,
    string? Status = null);
