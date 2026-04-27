using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Services;
using PokemonChampions.Data.Entities;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Extensions;

namespace PokemonChampions.Data.Services;

public class AbilityService(AppDbContext db, ISettingsService settings, FormatRegistry formatRegistry) : IAbilityService
{
    public async Task<Ability?> FindAsync(string nameOrAlias, CancellationToken ct = default)
    {
        var normalized = nameOrAlias.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);

        var entity = await db.Abilities
            .FirstOrDefaultAsync(a => a.NormalizedId == normalized, ct);
        if (entity is not null) return WithFormat(MapToDomain(entity), format);

        var alias = await db.Aliases.FirstOrDefaultAsync(
            a => a.AliasText == nameOrAlias && a.TargetType == "ability", ct);
        if (alias is not null)
        {
            entity = await db.Abilities.FindAsync([alias.TargetId], ct);
            if (entity is not null) return WithFormat(MapToDomain(entity), format);
        }

        return null;
    }

    public async Task<IReadOnlyList<Ability>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        var normalized = query.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);
        var candidates = await db.Abilities
            .Where(a => a.NormalizedId.Contains(normalized))
            .OrderBy(a => a.NormalizedId)
            .Take(maxResults * 3)
            .ToListAsync(ct);

        return candidates
            .OrderBy(a => a.NormalizedId.LevenshteinDistance(normalized))
            .Take(maxResults)
            .Select(e => WithFormat(MapToDomain(e), format))
            .ToList();
    }

    internal static Ability MapToDomain(AbilityEntity e) => new()
    {
        ShowdownId = e.ShowdownId,
        Name = e.Name,
        ShortDesc = e.ShortDesc,
        Desc = e.Desc
    };

    private async Task<IFormatDefinition?> GetCurrentFormatAsync(CancellationToken ct)
    {
        var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);
        return formatId is null ? null : formatRegistry.TryGet(formatId);
    }

    private static Ability WithFormat(Ability a, IFormatDefinition? format)
    {
        a.IsLegalInCurrentFormat = format is null || format.IsAbilityAllowed(a.ShowdownId);
        return a;
    }
}
