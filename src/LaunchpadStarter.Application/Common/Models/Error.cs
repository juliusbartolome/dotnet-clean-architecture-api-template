namespace LaunchpadStarter.Application.Common.Models;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("validation.failed", message);

    public static Error NotFound(string message) => new("catalog.not_found", message);

    public static Error Conflict(string message) => new("catalog.conflict", message);
}
