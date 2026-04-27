using Microsoft.EntityFrameworkCore;
using PokemonChampions.Data;
using PokemonChampions.Import.Importers;
using Spectre.Console;

namespace PokemonChampions.Cli.Commands;

/// <summary>
/// Handles the 'update' command: fetches fresh Pokemon/move/item/ability/learnset data
/// from Pokemon Showdown and stores it in the local database.
/// </summary>
public class UpdateCommand(StaticDataImporter importer)
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

                await importer.ImportAllAsync(progress, ct);
            });

        AnsiConsole.MarkupLine("[green]✓[/] Local data updated successfully.");

        if (online)
        {
            AnsiConsole.MarkupLine("[yellow]Online usage stats sync is not yet implemented.[/]");
        }
    }
}
