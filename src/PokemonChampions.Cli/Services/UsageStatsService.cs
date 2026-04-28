using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Services;
using PokemonChampions.Data;
using PokemonChampions.Import.Importers;
using PokemonChampions.Shared.Constants;

namespace PokemonChampions.Cli.Services;

/// <summary>
/// Implements <see cref="IUsageStatsService"/>.
/// In offline mode: returns only data already cached in the local DB.
/// In online mode: lazily fetches per-Pokemon detail (moves + teammates) from MunchStats on first
/// access, then serves from the DB cache (refreshed if older than <see cref="DetailCacheTtlDays"/> days).
/// </summary>
public class UsageStatsService(
    AppDbContext db,
    ISettingsService settings,
    FormatRegistry formatRegistry,
    UsageStatsImporter importer) : IUsageStatsService
{
    private const int DetailCacheTtlDays = 7;

    public bool IsOnlineModeEnabled { get; private set; }

    public async Task<PokemonUsageStats?> GetAsync(
        string pokemonShowdownId,
        string formatShowdownId,
        CancellationToken ct = default)
    {
        IsOnlineModeEnabled = await IsOnlineAsync(ct);

        var pokemonEntity = await db.Pokemon
            .FirstOrDefaultAsync(p => p.ShowdownId == pokemonShowdownId, ct);
        if (pokemonEntity is null) return null;

        var statsEntity = await db.UsageStats
            .Where(u => u.PokemonId == pokemonEntity.Id &&
                        u.FormatShowdownId == formatShowdownId &&
                        u.Source == "munchstats")
            .FirstOrDefaultAsync(ct);

        // In online mode, lazily fetch moves + teammates if missing or stale.
        // Staleness is keyed on teammate presence so existing caches (moves only) are refreshed.
        if (IsOnlineModeEnabled && statsEntity is not null)
        {
            bool detailStale = !await db.UsageTeammates.AnyAsync(t => t.StatsId == statsEntity.Id, ct)
                               || statsEntity.FetchedAt < DateTime.UtcNow.AddDays(-DetailCacheTtlDays);

            if (detailStale)
            {
                var format = formatRegistry.TryGet(formatShowdownId);
                if (format?.MunchStatsFormatId is { } munchFormatId)
                    await importer.ImportPokemonDetailAsync(
                        statsEntity.Id, munchFormatId, pokemonEntity.Name, ct);

                statsEntity = await db.UsageStats
                    .Where(u => u.Id == statsEntity.Id)
                    .FirstOrDefaultAsync(ct);
            }
        }

        if (statsEntity is null) return null;

        var moves = await db.UsageMoves
            .Where(m => m.StatsId == statsEntity.Id)
            .Include(m => m.Move)
            .OrderBy(m => m.Rank)
            .ToListAsync(ct);

        var teammates = await db.UsageTeammates
            .Where(t => t.StatsId == statsEntity.Id)
            .Include(t => t.Teammate)
            .OrderBy(t => t.Rank)
            .ToListAsync(ct);

        return new PokemonUsageStats
        {
            PokemonShowdownId = pokemonShowdownId,
            FormatShowdownId  = formatShowdownId,
            UsagePct          = statsEntity.UsagePct,
            StatsMonth        = statsEntity.StatsMonth,
            Source            = statsEntity.Source,
            Moves = moves.Select((m, i) => new UsageEntry(
                m.Move.Name, m.Move.ShowdownId, m.UsagePct, i + 1)).ToList(),
            Teammates = teammates.Select((t, i) => new UsageEntry(
                t.Teammate.Name, t.Teammate.ShowdownId, t.UsagePct, i + 1)).ToList()
        };
    }

    private async Task<bool> IsOnlineAsync(CancellationToken ct)
    {
        var val = await settings.GetAsync(AppConstants.SettingKeys.OnlineMode, ct);
        return val?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }
}
