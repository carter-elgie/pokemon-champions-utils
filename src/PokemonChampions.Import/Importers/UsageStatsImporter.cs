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
            if (!seenIds.Add(pokemonEntity.Id)) continue;

            var existing = await db.UsageStats.FirstOrDefaultAsync(u =>
                u.PokemonId == pokemonEntity.Id &&
                u.FormatShowdownId == appFormatId &&
                u.Source == "munchstats", ct);

            if (existing is null)
            {
                db.UsageStats.Add(new UsageStatsEntity
                {
                    PokemonId        = pokemonEntity.Id,
                    FormatShowdownId = appFormatId,
                    UsagePct         = usagePct,
                    StatsMonth       = month,
                    Source           = "munchstats",
                    FetchedAt        = now
                });
            }
            else
            {
                existing.UsagePct   = usagePct;
                existing.StatsMonth = month;
                existing.FetchedAt  = now;
            }

            imported++;
        }

        await db.SaveChangesAsync(ct);
        return imported;
    }

    /// <summary>
    /// Fetches and caches per-Pokemon detail (moves and teammates) from MunchStats.
    /// Replaces all existing move and teammate rows for this stats record.
    /// No-ops if the Pokemon page cannot be retrieved.
    /// </summary>
    public async Task ImportPokemonDetailAsync(
        int usageStatsId,
        string munchStatsFormatId,
        string pokemonDisplayName,
        CancellationToken ct)
    {
        var result = await munchStats.GetPokemonDetailAsync(munchStatsFormatId, pokemonDisplayName, ct);
        if (result is null) return;

        // ── Moves ──────────────────────────────────────────────────────────────
        var oldMoves = await db.UsageMoves.Where(m => m.StatsId == usageStatsId).ToListAsync(ct);
        db.UsageMoves.RemoveRange(oldMoves);

        int moveRank = 1;
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
                Rank     = moveRank++
            });
        }

        // ── Teammates ──────────────────────────────────────────────────────────
        var oldTeammates = await db.UsageTeammates.Where(t => t.StatsId == usageStatsId).ToListAsync(ct);
        db.UsageTeammates.RemoveRange(oldTeammates);

        int tmRank = 1;
        var seenTeammates = new HashSet<int>();
        foreach (var (tmName, tmPct) in result.Teammates)
        {
            var normalized = tmName.ToNormalizedId();
            var tmEntity = await db.Pokemon.FirstOrDefaultAsync(p => p.NormalizedId == normalized, ct);
            if (tmEntity is null) continue;
            if (!seenTeammates.Add(tmEntity.Id)) continue;

            db.UsageTeammates.Add(new UsageTeammateEntity
            {
                StatsId     = usageStatsId,
                TeammateId  = tmEntity.Id,
                UsagePct    = tmPct,
                Rank        = tmRank++
            });
        }

        // ── Update timestamp ───────────────────────────────────────────────────
        var statsEntity = await db.UsageStats.FindAsync([usageStatsId], ct);
        if (statsEntity is not null)
        {
            statsEntity.FetchedAt = DateTime.UtcNow;
            if (result.Month is not null) statsEntity.StatsMonth = result.Month;
        }

        await db.SaveChangesAsync(ct);
    }
}
