using Microsoft.AspNetCore.Authorization;

namespace UniEnroll.Application.Security;

public sealed class PrereqWaiverHandler : AuthorizationHandler<PrereqWaiverRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PrereqWaiverRequirement requirement)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Registrar"))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
