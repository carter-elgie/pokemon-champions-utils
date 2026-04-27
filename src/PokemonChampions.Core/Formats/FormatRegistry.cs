namespace PokemonChampions.Core.Formats;

/// <summary>
/// Central registry of all known format definitions.
/// Register new formats by calling <see cref="Register"/> at application startup.
/// </summary>
public class FormatRegistry
{
    private readonly Dictionary<string, IFormatDefinition> _formats = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IFormatDefinition format) =>
        _formats[format.ShowdownId] = format;

    /// <summary>Returns the format with the given Showdown ID, or throws if not found.</summary>
    public IFormatDefinition Get(string showdownId)
    {
        if (_formats.TryGetValue(showdownId, out var format)) return format;
        throw new KeyNotFoundException($"Format '{showdownId}' is not registered. Did you forget to register it at startup?");
    }

    /// <summary>Returns the format with the given Showdown ID, or null if not registered.</summary>
    public IFormatDefinition? TryGet(string showdownId) =>
        _formats.TryGetValue(showdownId, out var f) ? f : null;

    public bool IsRegistered(string showdownId) => _formats.ContainsKey(showdownId);

    public IReadOnlyList<IFormatDefinition> All => [.. _formats.Values];
}
