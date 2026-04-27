using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Services;
using PokemonChampions.Shared.Constants;
using Spectre.Console;

namespace PokemonChampions.Cli.Commands;

public class ConfigCommand(ISettingsService settings, FormatRegistry formatRegistry)
{
    public async Task RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            await ShowCurrentConfigAsync(ct);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "--online":
                await settings.SetBoolAsync(AppConstants.SettingKeys.OnlineMode, true, ct);
                AnsiConsole.MarkupLine("[green]Online mode enabled.[/] Usage statistics will be fetched live.");
                break;

            case "--offline":
                await settings.SetBoolAsync(AppConstants.SettingKeys.OnlineMode, false, ct);
                AnsiConsole.MarkupLine("[grey]Offline mode enabled.[/] Only locally cached data will be used.");
                break;

            case "--format":
                if (args.Length < 2)
                {
                    AnsiConsole.MarkupLine("[red]Usage:[/] config --format <format-id>");
                    PrintAvailableFormats();
                    return;
                }
                var formatId = args[1];
                if (!formatRegistry.IsRegistered(formatId))
                {
                    AnsiConsole.MarkupLine($"[red]Unknown format:[/] {Markup.Escape(formatId)}");
                    PrintAvailableFormats();
                    return;
                }
                await settings.SetAsync(AppConstants.SettingKeys.CurrentFormat, formatId, ct);
                var fmt = formatRegistry.Get(formatId);
                AnsiConsole.MarkupLine($"[green]Format set:[/] {Markup.Escape(fmt.DisplayName)}");
                break;

            default:
                AnsiConsole.MarkupLine($"[red]Unknown option:[/] {Markup.Escape(args[0])}");
                AnsiConsole.MarkupLine("Usage: config [--online | --offline | --format <id>]");
                break;
        }
    }

    private async Task ShowCurrentConfigAsync(CancellationToken ct)
    {
        var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct) ?? "(none)";
        var online = await settings.GetBoolAsync(AppConstants.SettingKeys.OnlineMode, false, ct);

        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Setting")
            .AddColumn("Value");

        table.AddRow("Format", formatId);
        table.AddRow("Mode", online ? "[green]online[/]" : "[grey]offline[/]");

        AnsiConsole.Write(table);
    }

    private void PrintAvailableFormats()
    {
        AnsiConsole.MarkupLine("[grey]Available formats:[/]");
        foreach (var f in formatRegistry.All)
            AnsiConsole.MarkupLine($"  [bold]{Markup.Escape(f.ShowdownId)}[/]  {Markup.Escape(f.DisplayName)}");
    }
}
