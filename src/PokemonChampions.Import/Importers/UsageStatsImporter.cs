using Microsoft.EntityFrameworkCore;
using PokemonChampions.Data;
using PokemonChampions.Data.Entities;
using PokemonChampions.Import.Sources;
using PokemonChampions.Shared.Extensions;

namespace PokemonChampions.Import.Importers;

/// <summary>
/// Fetches usage data from MunchStats and upserts it into the local database.
/// </summary>
public class UsageStatsImporter(AppDbContext db, MunchStatsSource munchStats)
{
    /// <summary>
    /// Fetches the full Pokemon usage list for a format (one HTTP request) and
    /// upserts overall usage percentages for every recognised Pokemon.
    /// Returns the number of Pokemon whose data was updated.
    /// </summary>
    public async Task<int> ImportFormatUsageAsync(
        string appFormatId,
        string munchStatsFormatId,
        CancellationToken ct)
    {
        var result = await munchStats.GetFormatUsageListAsync(munchStatsFormatId, ct);
        var month = result.Month ?? "live";
        var now = DateTime.UtcNow;
        int imported = 0;
        var seenIds = new HashSet<int>();

        foreach (var (displayName, usagePct) in result.Pokemon)
        {
            var normalized = displayName.ToNormalizedId();
            var pokemonEntity = await db.Pokemon
                .FirstOrDefaultAsync(p => p.NormalizedId == normalized, ct);
            if (pokemonEntity is null) continue;
            if (!seenIds.Add(pokemonEntity.Id)) continue; // skip if two names resolve to same Pokemon

            var existing = await db.UsageStats.FirstOrDefaultAsync(u =>
                u.PokemonId == pokemonEntity.Id &&
                u.FormatShowdownId == appFormatId &&
                u.Source == "munchstats", ct);

            if (existing is null)
            {
                db.UsageStats.Add(new UsageStatsEntity
                {
                    PokemonId       = pokemonEntity.Id,
                    FormatShowdownId = appFormatId,
                    UsagePct        = usagePct,
                    StatsMonth      = month,
                    Source          = "munchstats",
                    FetchedAt       = now
                });
            }
            else
            {
                existing.UsagePct  = usagePct;
                existing.StatsMonth = month;
                existing.FetchedAt  = now;
            }

            imported++;
        }

        await db.SaveChangesAsync(ct);
        return imported;
    }

    /// <summary>
    /// Fetches and caches move usage data for a single Pokemon.
    /// Replaces any existing move rows for this stats record.
    /// No-ops if the Pokemon cannot be found on MunchStats.
    /// </summary>
    public async Task ImportPokemonMovesAsync(
        int usageStatsId,
        string munchStatsFormatId,
        string pokemonDisplayName,
        CancellationToken ct)
    {
        var result = await munchStats.GetPokemonMovesAsync(munchStatsFormatId, pokemonDisplayName, ct);
        if (result is null || result.Moves.Count == 0) return;

        // Remove stale move rows
        var old = await db.UsageMoves.Where(m => m.StatsId == usageStatsId).ToListAsync(ct);
        db.UsageMoves.RemoveRange(old);

        int rank = 1;
        foreach (var (moveName, movePct) in result.Moves)
        {
            var normalized = moveName.ToNormalizedId();
            var moveEntity = await db.Moves.FirstOrDefaultAsync(m => m.NormalizedId == normalized, ct);
            if (moveEntity is null) continue;

            db.UsageMoves.Add(new UsageMoveEntity
            {
                StatsId  = usageStatsId,
                MoveId   = moveEntity.Id,
                UsagePct = movePct,
                Rank     = rank++
            });
        }

        // Update FetchedAt so we know moves have been loaded
        var statsEntity = await db.UsageStats.FindAsync([usageStatsId], ct);
        if (statsEntity is not null)
        {
            statsEntity.FetchedAt = DateTime.UtcNow;
            if (result.Month is not null) statsEntity.StatsMonth = result.Month;
        }

        await db.SaveChangesAsync(ct);
    }
}
