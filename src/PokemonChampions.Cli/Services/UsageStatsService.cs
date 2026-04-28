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
/// In online mode: lazily fetches per-Pokemon move data from MunchStats on first access,
/// then serves from the DB cache (refreshed if older than <see cref="MoveCacheTtlDays"/> days).
/// </summary>
public class UsageStatsService(
    AppDbContext db,
    ISettingsService settings,
    FormatRegistry formatRegistry,
    UsageStatsImporter importer) : IUsageStatsService
{
    private const int MoveCacheTtlDays = 7;

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

        // In online mode, lazily populate move data if missing or stale
        if (IsOnlineModeEnabled && statsEntity is not null)
        {
            bool movesStale = !await db.UsageMoves.AnyAsync(m => m.StatsId == statsEntity.Id, ct)
                              || statsEntity.FetchedAt < DateTime.UtcNow.AddDays(-MoveCacheTtlDays);

            if (movesStale)
            {
                var format = formatRegistry.TryGet(formatShowdownId);
                var munchFormatId = format?.MunchStatsFormatId;
                if (munchFormatId is not null)
                    await importer.ImportPokemonMovesAsync(
                        statsEntity.Id, munchFormatId, pokemonEntity.Name, ct);

                // Re-query to pick up newly inserted moves
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

        return new PokemonUsageStats
        {
            PokemonShowdownId = pokemonShowdownId,
            FormatShowdownId  = formatShowdownId,
            UsagePct          = statsEntity.UsagePct,
            StatsMonth        = statsEntity.StatsMonth,
            Source            = statsEntity.Source,
            Moves = moves.Select((m, i) => new UsageEntry(
                m.Move.Name, m.Move.ShowdownId, m.UsagePct, i + 1)).ToList()
        };
    }

    private async Task<bool> IsOnlineAsync(CancellationToken ct)
    {
        var val = await settings.GetAsync(AppConstants.SettingKeys.OnlineMode, ct);
        return val?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }
}
