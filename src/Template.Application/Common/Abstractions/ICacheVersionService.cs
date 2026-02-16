namespace Template.Application.Common.Abstractions;

public interface ICacheVersionService
{
    Task<string> GetVersionAsync(string key, CancellationToken cancellationToken = default);

    Task<string> BumpVersionAsync(string key, CancellationToken cancellationToken = default);
}
