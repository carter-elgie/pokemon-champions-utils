using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Services;

/// <summary>Provides access to Pokemon species data from the local database.</summary>
public interface IPokemonService
{
    /// <summary>
    /// Resolves a user input string to a Pokemon, checking aliases and fuzzy matching.
    /// Returns null if no match found.
    /// </summary>
    Task<Pokemon?> FindAsync(string nameOrAlias, CancellationToken ct = default);

    Task<IReadOnlyList<Pokemon>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);

    /// <summary>Returns the full stat ranges for a Pokemon at level 50 with 31 IVs.</summary>
    StatRange GetStatRange(int baseStat, StatName stat);

    /// <summary>Returns all stat ranges for a Pokemon species.</summary>
    IReadOnlyDictionary<StatName, StatRange> GetAllStatRanges(BaseStats baseStats);
}
