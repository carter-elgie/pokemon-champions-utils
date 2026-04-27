using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Legality;

namespace PokemonChampions.Core.Services;

public interface ITeamService
{
    Task<IReadOnlyList<Team>> GetAllAsync(string? formatShowdownId = null, CancellationToken ct = default);
    Task<Team?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Team?> GetActiveAsync(CancellationToken ct = default);
    Task<Team> ImportPokepasteAsync(string name, string pokepaste, string? formatShowdownId = null, CancellationToken ct = default);
    Task<Team> UpdatePokepasteAsync(string name, string newPokepaste, CancellationToken ct = default);
    Task SetActiveAsync(string name, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
    Task<LegalityReport> ValidateAsync(int teamId, string formatShowdownId, CancellationToken ct = default);
}
