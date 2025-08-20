using Microsoft.AspNetCore.Authorization;

namespace UniEnroll.Application.Security;

// Marker requirements if you later add richer business logic (reason codes, etc.)
public sealed class PrereqWaiverRequirement : IAuthorizationRequirement { }
public sealed class CapacityOverrideRequirement(long? DepartmentId = null) : IAuthorizationRequirement
{
    public long? DepartmentId { get; } = DepartmentId;
}