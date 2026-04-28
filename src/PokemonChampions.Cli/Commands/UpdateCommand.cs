using PokemonChampions.Core.Formats;
using PokemonChampions.Import.Importers;
using PokemonChampions.Shared.Constants;
using Spectre.Console;

namespace PokemonChampions.Cli.Commands;

/// <summary>
/// Handles the 'update' command: fetches fresh Pokemon/move/item/ability/learnset data
/// from Pokemon Showdown, and optionally fetches usage statistics from MunchStats.
/// </summary>
public class UpdateCommand(StaticDataImporter staticImporter, UsageStatsImporter usageImporter, FormatRegistry formatRegistry)
{
    /// <summary>Called from the REPL dispatcher; parses --online from the remaining tokens.</summary>
    public async Task RunFromReplAsync(string[] args, CancellationToken ct)
    {
        bool online = args.Any(a => a.Equals("--online", StringComparison.OrdinalIgnoreCase));
        await RunCoreAsync(online, ct);
    }

    private async Task RunCoreAsync(bool online, CancellationToken ct)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Updating local data[/]", maxValue: 5);

                var progress = new Progress<string>(msg =>
                {
                    AnsiConsole.MarkupLine($"  [dim]{Markup.Escape(msg)}[/]");
                    task.Increment(1);
                });

                await staticImporter.ImportAllAsync(progress, ct);
            });

        AnsiConsole.MarkupLine("[green]✓[/] Local data updated successfully.");

        if (!online) return;

        AnsiConsole.MarkupLine("[dim]Fetching usage statistics from MunchStats...[/]");
        int total = 0;
        foreach (var format in formatRegistry.All)
        {
            if (format.MunchStatsFormatId is null) continue;
            try
            {
                int count = await usageImporter.ImportFormatUsageAsync(
                    format.ShowdownId, format.MunchStatsFormatId, ct);
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] {Markup.Escape(format.DisplayName)}: {count} Pokemon usage percentages cached.");
                total += count;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]⚠ Usage stats unavailable for {Markup.Escape(format.DisplayName)}: {Markup.Escape(ex.Message)}[/]");
            }
        }
        if (total > 0)
            AnsiConsole.MarkupLine("[dim]Move/item data will be fetched on first Pokemon lookup.[/]");
    }
}
