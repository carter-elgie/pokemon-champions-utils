using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Services;

/// <summary>
/// Provides usage statistics for Pokemon in a given format.
/// Returns null when in offline mode or when data is unavailable.
/// </summary>
public interface IUsageStatsService
{
    bool IsOnlineModeEnabled { get; }

    Task<PokemonUsageStats?> GetAsync(
        string pokemonShowdownId,
        string formatShowdownId,
        CancellationToken ct = default);
}
