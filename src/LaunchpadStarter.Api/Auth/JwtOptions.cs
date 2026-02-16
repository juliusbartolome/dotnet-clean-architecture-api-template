namespace LaunchpadStarter.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "LaunchpadStarter.Api";

    public string Audience { get; init; } = "LaunchpadStarter.Client";

    public string SigningKey { get; init; } = "replace-with-long-dev-key-please-change";
}
