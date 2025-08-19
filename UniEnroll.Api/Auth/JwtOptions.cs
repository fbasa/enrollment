namespace UniEnroll.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "UniEnroll";
    public string Audience { get; init; } = "UniEnroll.Client";
    public string Key { get; init; } = "DEV_KEY_ONLY";
}

