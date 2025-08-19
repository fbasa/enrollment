using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UniEnroll.Api.Filters;

// Note: [ApiController] already auto-400s. Keep this available if you prefer manual control.
public sealed class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
            context.Result = new BadRequestObjectResult(context.ModelState);
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}
