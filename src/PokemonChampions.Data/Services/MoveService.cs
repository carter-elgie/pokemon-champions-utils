using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Services;
using PokemonChampions.Data.Entities;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Enums;
using PokemonChampions.Shared.Extensions;

namespace PokemonChampions.Data.Services;

public class MoveService(AppDbContext db, ISettingsService settings, FormatRegistry formatRegistry) : IMoveService
{
    public async Task<Move?> FindAsync(string nameOrAlias, CancellationToken ct = default)
    {
        var normalized = nameOrAlias.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);

        var entity = await db.Moves
            .FirstOrDefaultAsync(m => m.NormalizedId == normalized, ct);
        if (entity is not null) return WithFormat(MapToDomain(entity), format);

        var alias = await db.Aliases.FirstOrDefaultAsync(
            a => a.AliasText == nameOrAlias && a.TargetType == "move", ct);
        if (alias is not null)
        {
            entity = await db.Moves.FindAsync([alias.TargetId], ct);
            if (entity is not null) return WithFormat(MapToDomain(entity), format);
        }

        return null;
    }

    public async Task<IReadOnlyList<Move>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        var normalized = query.ToNormalizedId();
        var format = await GetCurrentFormatAsync(ct);
        var candidates = await db.Moves
            .Where(m => m.NormalizedId.Contains(normalized))
            .OrderBy(m => m.NormalizedId)
            .Take(maxResults * 3)
            .ToListAsync(ct);

        return candidates
            .OrderBy(m => m.NormalizedId.LevenshteinDistance(normalized))
            .Take(maxResults)
            .Select(e => WithFormat(MapToDomain(e), format))
            .ToList();
    }

    public async Task<IReadOnlyList<Move>> GetLearnsetAsync(string pokemonShowdownId, string? formatShowdownId = null, CancellationToken ct = default)
    {
        var format = await GetCurrentFormatAsync(ct);
        var moves = await db.Learnsets
            .Where(l => l.Pokemon.ShowdownId == pokemonShowdownId)
            .Select(l => l.Move)
            .Distinct()
            .ToListAsync(ct);

        return moves.Select(e => WithFormat(MapToDomain(e), format)).ToList();
    }

    internal static Move MapToDomain(MoveEntity e) => new()
    {
        ShowdownId = e.ShowdownId,
        Name = e.Name,
        Type = ParseType(e.Type),
        Category = ParseCategory(e.Category),
        Power = e.Power,
        Accuracy = e.Accuracy,
        Pp = e.Pp,
        Priority = e.Priority,
        Target = e.Target,
        ShortDesc = e.ShortDesc,
        Desc = e.Desc
    };

    private async Task<IFormatDefinition?> GetCurrentFormatAsync(CancellationToken ct)
    {
        var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);
        return formatId is null ? null : formatRegistry.TryGet(formatId);
    }

    private static Move WithFormat(Move m, IFormatDefinition? format)
    {
        m.IsLegalInCurrentFormat = format is null || format.IsMoveAllowed(m.ShowdownId);
        return m;
    }

    private static PokemonType ParseType(string? t) =>
        t is not null && Enum.TryParse<PokemonType>(t, ignoreCase: true, out var type) ? type : PokemonType.Normal;

    private static MoveCategory ParseCategory(string? c) =>
        c is not null && Enum.TryParse<MoveCategory>(c, ignoreCase: true, out var cat) ? cat : MoveCategory.Status;
}
