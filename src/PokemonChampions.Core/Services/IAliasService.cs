using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Services;

public interface IAliasService
{
    Task CreateAsync(string aliasText, EntityType targetType, string targetShowdownId, CancellationToken ct = default);
    Task<bool> DeleteAsync(string aliasText, CancellationToken ct = default);

    /// <summary>
    /// Resolves an alias text to (targetType, targetId), or returns null if no alias exists.
    /// </summary>
    Task<(EntityType Type, int Id)?> ResolveAsync(string aliasText, CancellationToken ct = default);

    Task<IReadOnlyList<(string AliasText, EntityType Type, int TargetId)>> GetAllAsync(CancellationToken ct = default);
}
