using Microsoft.AspNetCore.Authorization;

namespace UniEnroll.Api.Security;

// Marker requirements if you later add richer business logic (reason codes, etc.)
public sealed class CapacityOverrideRequirement : IAuthorizationRequirement { }
public sealed class PrereqWaiverRequirement : IAuthorizationRequirement { }
