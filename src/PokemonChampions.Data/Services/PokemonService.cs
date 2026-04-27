using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Calculation;
using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Services;
using PokemonChampions.Data.Entities;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Enums;
using PokemonChampions.Shared.Extensions;

namespace PokemonChampions.Data.Services;

public class PokemonService(AppDbContext db, ISettingsService settings, FormatRegistry formatRegistry) : IPokemonService
{
    public async Task<Pokemon?> FindAsync(string nameOrAlias, CancellationToken ct = default)
    {
        var normalized = nameOrAlias.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);

        var entity = await db.Pokemon
            .FirstOrDefaultAsync(p => p.NormalizedId == normalized, ct);
        if (entity is not null) return WithFormat(MapToDomain(entity), format);

        var alias = await db.Aliases.FirstOrDefaultAsync(
            a => a.AliasText == nameOrAlias && a.TargetType == "pokemon", ct);
        if (alias is not null)
        {
            entity = await db.Pokemon.FindAsync([alias.TargetId], ct);
            if (entity is not null) return WithFormat(MapToDomain(entity), format);
        }

        return null;
    }

    public async Task<IReadOnlyList<Pokemon>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        var normalized = query.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);
        var candidates = await db.Pokemon
            .Where(p => p.NormalizedId.Contains(normalized))
            .OrderBy(p => p.NormalizedId)
            .Take(maxResults * 3)
            .ToListAsync(ct);

        return candidates
            .OrderBy(p => p.NormalizedId.LevenshteinDistance(normalized))
            .Take(maxResults)
            .Select(e => WithFormat(MapToDomain(e), format))
            .ToList();
    }

    public StatRange GetStatRange(int baseStat, StatName stat) =>
        StatCalculator.GetRange(baseStat, stat);

    public IReadOnlyDictionary<StatName, StatRange> GetAllStatRanges(BaseStats baseStats) =>
        StatCalculator.GetAllRanges(baseStats);

    internal static Pokemon MapToDomain(PokemonEntity e) => new()
    {
        ShowdownId = e.ShowdownId,
        Name = e.Name,
        Type1 = ParseType(e.Type1),
        Type2 = e.Type2 is not null ? ParseType(e.Type2) : null,
        BaseStats = new BaseStats(e.BaseHp, e.BaseAtk, e.BaseDef, e.BaseSpa, e.BaseSpd, e.BaseSpe),
        Ability0 = e.Ability0,
        Ability1 = e.Ability1,
        AbilityH = e.AbilityH,
        IsMega = e.IsMega,
        BaseFormShowdownId = e.BaseFormShowdownId
    };

    private async Task<IFormatDefinition?> GetCurrentFormatAsync(CancellationToken ct)
    {
        var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);
        return formatId is null ? null : formatRegistry.TryGet(formatId);
    }

    private static Pokemon WithFormat(Pokemon p, IFormatDefinition? format)
    {
        p.IsLegalInCurrentFormat = format is null || format.IsPokemonAllowed(p.ShowdownId);
        return p;
    }

    private static PokemonType ParseType(string? t) =>
        t is not null && Enum.TryParse<PokemonType>(t, ignoreCase: true, out var type) ? type : PokemonType.None;
}
