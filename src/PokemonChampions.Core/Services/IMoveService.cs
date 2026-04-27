using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Services;

public interface IMoveService
{
    Task<Move?> FindAsync(string nameOrAlias, CancellationToken ct = default);
    Task<IReadOnlyList<Move>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
    Task<IReadOnlyList<Move>> GetLearnsetAsync(string pokemonShowdownId, string? formatShowdownId = null, CancellationToken ct = default);
}
