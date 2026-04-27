using PokemonChampions.Cli.Rendering;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Legality;
using PokemonChampions.Core.Services;
using PokemonChampions.Shared.Constants;
using Spectre.Console;

namespace PokemonChampions.Cli.Commands;

public class TeamsCommand(ITeamService teamService, ISettingsService settings, FormatRegistry formatRegistry)
{
    public async Task RunAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            await ListTeamsAsync(ct);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "new":
                await NewTeamAsync(args[1..], ct);
                break;
            case "edit":
                await EditTeamAsync(JoinName(args[1..]), ct);
                break;
            case "set":
                await SetActiveAsync(JoinName(args[1..]), ct);
                break;
            case "remove":
            case "delete":
                await RemoveTeamAsync(JoinName(args[1..]), ct);
                break;
            case "show":
            case "view":
                await ShowTeamAsync(JoinName(args[1..]), ct);
                break;
            case "validate":
                await ValidateTeamAsync(JoinName(args[1..]), ct);
                break;
            default:
                // Treat as team name: show that team
                await ShowTeamAsync(JoinName(args), ct);
                break;
        }
    }

    // ── Subcommands ───────────────────────────────────────────────────────────

    private async Task ListTeamsAsync(CancellationToken ct)
    {
        var formatId  = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);
        var activeName = await settings.GetAsync(AppConstants.SettingKeys.ActiveTeamName, ct);
        var teams = await teamService.GetAllAsync(null, ct);

        if (teams.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No teams saved. Use [bold]teams new[/] to create one.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[grey]{teams.Count} team(s)[/]");
        foreach (var t in teams)
            TeamRenderer.RenderRow(t, string.Equals(t.Name, activeName, StringComparison.OrdinalIgnoreCase));
        AnsiConsole.WriteLine();
    }

    private async Task ShowTeamAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var active = await teamService.GetActiveAsync(ct);
            if (active is null) { AnsiConsole.MarkupLine("[grey]No active team set.[/]"); return; }
            name = active.Name;
        }

        var team = await teamService.GetByNameAsync(name, ct);
        if (team is null) { AnsiConsole.MarkupLine($"[red]Team not found:[/] {Markup.Escape(name)}"); return; }

        var activeName = await settings.GetAsync(AppConstants.SettingKeys.ActiveTeamName, ct);

        // Build a report from stored legality data (no re-validation)
        var storedReport = BuildStoredReport(team);
        TeamRenderer.Render(team, storedReport, activeName);
    }

    private async Task NewTeamAsync(string[] args, CancellationToken ct)
    {
        string? name = null;
        string? filePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--name", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                name = args[++i];
            else if (args[i].Equals("--file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                filePath = args[++i];
        }

        if (name is null)
        {
            Console.Write("Team name: ");
            name = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { AnsiConsole.MarkupLine("[grey]Cancelled.[/]"); return; }
        }

        string pokepaste;
        if (filePath is not null)
        {
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine($"[red]File not found:[/] {Markup.Escape(filePath)}");
                return;
            }
            pokepaste = await File.ReadAllTextAsync(filePath, ct);
        }
        else
        {
            pokepaste = ReadPokepasteFromConsole();
            if (string.IsNullOrWhiteSpace(pokepaste)) { AnsiConsole.MarkupLine("[grey]Cancelled.[/]"); return; }
        }

        var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);

        try
        {
            var team = await teamService.ImportPokepasteAsync(name, pokepaste, formatId, ct);
            AnsiConsole.MarkupLine($"[green]Team created:[/] {Markup.Escape(team.Name)} ({team.Members.Count} members)");

            // Auto-validate if a format is set
            if (formatId is not null && formatRegistry.IsRegistered(formatId))
                await ValidateAndReportAsync(team.Id, formatId, ct);
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private async Task EditTeamAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var active = await teamService.GetActiveAsync(ct);
            if (active is null) { AnsiConsole.MarkupLine("[grey]No active team. Specify a team name.[/]"); return; }
            name = active.Name;
        }

        AnsiConsole.MarkupLine($"Editing [bold]{Markup.Escape(name)}[/]. Paste new pokepaste below:");
        var pokepaste = ReadPokepasteFromConsole();
        if (string.IsNullOrWhiteSpace(pokepaste)) { AnsiConsole.MarkupLine("[grey]Cancelled.[/]"); return; }

        try
        {
            var team = await teamService.UpdatePokepasteAsync(name, pokepaste, ct);
            AnsiConsole.MarkupLine($"[green]Team updated:[/] {Markup.Escape(team.Name)} ({team.Members.Count} members)");

            var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);
            if (formatId is not null && formatRegistry.IsRegistered(formatId))
                await ValidateAndReportAsync(team.Id, formatId, ct);
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private async Task SetActiveAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) { AnsiConsole.MarkupLine("[red]Usage:[/] teams set <name>"); return; }
        try
        {
            await teamService.SetActiveAsync(name, ct);
            AnsiConsole.MarkupLine($"[green]Active team:[/] {Markup.Escape(name)}");
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }
    }

    private async Task RemoveTeamAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) { AnsiConsole.MarkupLine("[red]Usage:[/] teams remove <name>"); return; }

        var team = await teamService.GetByNameAsync(name, ct);
        if (team is null) { AnsiConsole.MarkupLine($"[red]Team not found:[/] {Markup.Escape(name)}"); return; }

        Console.Write($"Remove team '{name}'? (y/N) ");
        var response = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (response != "y" && response != "yes")
        {
            AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
            return;
        }

        await teamService.DeleteAsync(name, ct);
        AnsiConsole.MarkupLine($"[green]Removed:[/] {Markup.Escape(name)}");
    }

    private async Task ValidateTeamAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var active = await teamService.GetActiveAsync(ct);
            if (active is null) { AnsiConsole.MarkupLine("[grey]No active team. Specify a team name.[/]"); return; }
            name = active.Name;
        }

        var team = await teamService.GetByNameAsync(name, ct);
        if (team is null) { AnsiConsole.MarkupLine($"[red]Team not found:[/] {Markup.Escape(name)}"); return; }

        var formatId = await settings.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct);
        if (formatId is null || !formatRegistry.IsRegistered(formatId))
        {
            AnsiConsole.MarkupLine("[yellow]No format set.[/] Use [bold]config --format <id>[/] first.");
            return;
        }

        await ValidateAndReportAsync(team.Id, formatId, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task ValidateAndReportAsync(int teamId, string formatId, CancellationToken ct)
    {
        var report = await teamService.ValidateAsync(teamId, formatId, ct);
        if (report.IsLegal)
            AnsiConsole.MarkupLine("[green]✓ Team is legal.[/]");
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ {report.Violations.Count} violation(s):[/]");
            foreach (var v in report.Violations)
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(v.Message)}");
        }
        foreach (var w in report.Warnings)
            AnsiConsole.MarkupLine($"  [yellow]•[/] {Markup.Escape(w.Message)}");
    }

    private static string ReadPokepasteFromConsole()
    {
        AnsiConsole.MarkupLine("[grey]Paste your pokepaste. Enter a blank line to finish:[/]");
        var lines = new List<string>();

        while (true)
        {
            var line = Console.ReadLine();
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line))
            {
                // Two consecutive blank lines = done; one blank = separator between mons
                if (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
                    break;
                lines.Add(string.Empty);
            }
            else
            {
                lines.Add(line);
            }
        }

        return string.Join("\n", lines).TrimEnd();
    }

    private static LegalityReport? BuildStoredReport(Core.Domain.Team team)
    {
        // Reconstruct a report from the persisted IsLegal / LegalityNotes on each member
        var violations = team.Members
            .Where(m => m.IsLegal == false && m.LegalityNotes is not null)
            .SelectMany(m => m.LegalityNotes!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(msg => new LegalityViolation(msg.Trim(), m.SlotIndex)))
            .ToList();

        var warnings = team.Members
            .Where(m => m.IsLegal is null)
            .Select(m => new LegalityWarning($"Slot {m.SlotIndex + 1} has not been validated yet.", m.SlotIndex))
            .ToList();

        // Return null if no legality info at all (never validated)
        if (violations.Count == 0 && team.Members.All(m => m.IsLegal is null))
            return null;

        return new LegalityReport { Violations = violations, Warnings = warnings };
    }

    private static string JoinName(string[] tokens) => string.Join(" ", tokens);
}
