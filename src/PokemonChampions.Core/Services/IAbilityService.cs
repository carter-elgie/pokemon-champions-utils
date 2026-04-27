using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Services;

public interface IAbilityService
{
    Task<Ability?> FindAsync(string nameOrAlias, CancellationToken ct = default);
    Task<IReadOnlyList<Ability>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
}
