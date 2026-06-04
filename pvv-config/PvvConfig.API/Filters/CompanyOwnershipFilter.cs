using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PvvConfig.API.Filters;

/// <summary>
/// Ensures the companyId in the route matches the companyId claim in the JWT,
/// so an operator cannot read or modify another company's configurations.
/// </summary>
public class CompanyOwnershipFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var routeCompanyId = context.RouteData.Values["companyId"]?.ToString();
        var claimCompanyId = context.HttpContext.User.FindFirst("companyId")?.Value;

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
