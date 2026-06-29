using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PvvConfig.Domain.Enums;

namespace PvvConfig.API.Filters;

/// <summary>
/// Ensures the companyId in the route matches the companyId claim in the JWT,
/// so a company operator cannot read or modify another company's configurations.
/// System admins bypass this check (they manage every company).
/// </summary>
public class CompanyOwnershipFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        // System admins can access any company's configurations.
        if (user.FindFirst(ClaimTypes.Role)?.Value == nameof(OperatorRole.SystemAdmin))
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
                Detail = "You cannot access configurations of another company."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}
