namespace Template.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "Template.Api";

    public string Audience { get; init; } = "Template.Client";

    public string SigningKey { get; init; } = "replace-with-long-dev-key-please-change";
}
