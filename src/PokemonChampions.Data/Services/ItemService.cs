using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Services;
using PokemonChampions.Data.Entities;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Extensions;

namespace PokemonChampions.Data.Services;

public class ItemService(AppDbContext db, ISettingsService settings, FormatRegistry formatRegistry) : IItemService
{
    public async Task<Item?> FindAsync(string nameOrAlias, CancellationToken ct = default)
    {
        var normalized = nameOrAlias.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);

        var entity = await db.Items
            .FirstOrDefaultAsync(i => i.NormalizedId == normalized, ct);
        if (entity is not null) return WithFormat(MapToDomain(entity), format);

        var alias = await db.Aliases.FirstOrDefaultAsync(
            a => a.AliasText == nameOrAlias && a.TargetType == "item", ct);
        if (alias is not null)
        {
            entity = await db.Items.FindAsync([alias.TargetId], ct);
            if (entity is not null) return WithFormat(MapToDomain(entity), format);
        }

        return null;
    }

    public async Task<IReadOnlyList<Item>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        var normalized = query.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);
        var candidates = await db.Items
            .Where(i => i.NormalizedId.Contains(normalized))
            .Take(maxResults * 3)
            .ToListAsync(ct);

        return candidates
            .OrderBy(i => i.NormalizedId.LevenshteinDistance(normalized))
            .Take(maxResults)
            .Select(e => WithFormat(MapToDomain(e), format))
            .ToList();
    }

    internal static Item MapToDomain(ItemEntity e) => new()
    {
        ShowdownId = e.ShowdownId,
        Name = e.Name,
        ShortDesc = e.ShortDesc,
        Desc = e.Desc,
        IsMegaStone = e.IsMegaStone,
        MegaStoneFor = e.MegaStoneFor
    };

    private async Task<IFormatDefinition?> GetCurrentFormatAsync(CancellationToken ct)
    {
        var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);
        return formatId is null ? null : formatRegistry.TryGet(formatId);
    }

    private static Item WithFormat(Item i, IFormatDefinition? format)
    {
        i.IsLegalInCurrentFormat = format is null || format.IsItemAllowed(i.ShowdownId);
        return i;
    }
}
