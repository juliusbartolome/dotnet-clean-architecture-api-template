namespace LaunchpadStarter.Application.Common.Extensions;

public static class CacheKeys
{
    public const string CatalogSearchVersion = "catalog:search:version";

    public static string ProductById(Guid id) => $"catalog:product:{id:N}";

    public static string SearchProducts(string version, bool? isActive, decimal? minPrice, decimal? maxPrice, string? query, int page, int pageSize)
        => $"catalog:search:{version}:{isActive?.ToString() ?? "any"}:{minPrice?.ToString("F2") ?? "na"}:{maxPrice?.ToString("F2") ?? "na"}:{NormalizeQuery(query)}:{page}:{pageSize}";

    private static string NormalizeQuery(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "none";
        }

        return value.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
