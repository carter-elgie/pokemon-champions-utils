using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Services;
using PokemonChampions.Data.Entities;
using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Data.Services;

public class AliasService(AppDbContext db) : IAliasService
{
    public async Task CreateAsync(string aliasText, EntityType targetType, string targetShowdownId, CancellationToken ct = default)
    {
        int targetId = targetType switch
        {
            EntityType.Pokemon => (await db.Pokemon.FirstOrDefaultAsync(p => p.ShowdownId == targetShowdownId, ct))?.Id ?? 0,
            EntityType.Move    => (await db.Moves.FirstOrDefaultAsync(m => m.ShowdownId == targetShowdownId, ct))?.Id ?? 0,
            EntityType.Item    => (await db.Items.FirstOrDefaultAsync(i => i.ShowdownId == targetShowdownId, ct))?.Id ?? 0,
            EntityType.Ability => (await db.Abilities.FirstOrDefaultAsync(a => a.ShowdownId == targetShowdownId, ct))?.Id ?? 0,
            _                  => 0
        };

        var existing = await db.Aliases
            .FirstOrDefaultAsync(a => a.AliasText == aliasText, ct);
        if (existing is not null)
        {
            existing.TargetType = targetType.ToString().ToLowerInvariant();
            existing.TargetId = targetId;
        }
        else
        {
            db.Aliases.Add(new AliasEntity
            {
                AliasText = aliasText,
                TargetType = targetType.ToString().ToLowerInvariant(),
                TargetId = targetId,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(string aliasText, CancellationToken ct = default)
    {
        var alias = await db.Aliases
            .FirstOrDefaultAsync(a => a.AliasText == aliasText, ct);
        if (alias is null) return false;
        db.Aliases.Remove(alias);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(EntityType Type, int Id)?> ResolveAsync(string aliasText, CancellationToken ct = default)
    {
        var alias = await db.Aliases
            .FirstOrDefaultAsync(a => a.AliasText == aliasText, ct);
        if (alias is null) return null;
        if (Enum.TryParse<EntityType>(alias.TargetType, ignoreCase: true, out var type))
            return (type, alias.TargetId);
        return null;
    }

    public async Task<IReadOnlyList<(string AliasText, EntityType Type, int TargetId)>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.Aliases.OrderBy(a => a.AliasText).ToListAsync(ct);
        return rows
            .Select(a => (
                a.AliasText,
                Enum.TryParse<EntityType>(a.TargetType, ignoreCase: true, out var t) ? t : EntityType.Pokemon,
                a.TargetId))
            .ToList();
    }
}
