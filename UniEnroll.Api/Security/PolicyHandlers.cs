using Microsoft.AspNetCore.Authorization;

namespace UniEnroll.Api.Security;

public sealed class CapacityOverrideHandler : AuthorizationHandler<CapacityOverrideRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CapacityOverrideRequirement requirement)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Registrar"))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
