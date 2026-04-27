using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Services;

public interface IItemService
{
    Task<Item?> FindAsync(string nameOrAlias, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
}
