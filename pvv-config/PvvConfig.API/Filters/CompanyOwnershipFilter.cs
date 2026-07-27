using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PvvConfig.Domain.Enums;

namespace PvvConfig.API.Filters;

/// <summary>
/// Ensures the companyId in the route matches the companyId claim in the JWT, so
/// a company operator cannot reach another company's data.
///
/// By default a system admin passes through, which is right for endpoints that
/// ARE company administration (creating, listing, editing the companies
/// themselves). Set <see cref="AllowSystemAdmin"/> to false on endpoints that
/// serve a tenant's own data — appearance, products, pricing — which belong to
/// the company's operator, not to the platform administrator.
/// </summary>
public class CompanyOwnershipFilter : ActionFilterAttribute
{
    /// <summary>Whether a SystemAdmin bypasses the ownership check. Default true.</summary>
    public bool AllowSystemAdmin { get; set; } = true;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (AllowSystemAdmin
            && user.FindFirst(ClaimTypes.Role)?.Value == nameof(OperatorRole.SystemAdmin))
        {
            base.OnActionExecuting(context);
            return;
        }

        var routeCompanyId = context.RouteData.Values["companyId"]?.ToString();
        var claimCompanyId = user.FindFirst("companyId")?.Value;

        if (string.IsNullOrEmpty(routeCompanyId)
            || string.IsNullOrEmpty(claimCompanyId)
            || !string.Equals(routeCompanyId, claimCompanyId, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "This data belongs to a company you do not operate."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}
