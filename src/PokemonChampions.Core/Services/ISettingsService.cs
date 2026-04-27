namespace PokemonChampions.Core.Services;

/// <summary>Provides access to persistent application settings stored in the local database.</summary>
public interface ISettingsService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken ct = default);
    Task SetBoolAsync(string key, bool value, CancellationToken ct = default);
}
