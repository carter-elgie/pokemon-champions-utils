using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Legality;
using Spectre.Console;

namespace PokemonChampions.Cli.Rendering;

public static class TeamRenderer
{
    /// <summary>Shows a compact team listing row (for 'teams' command output).</summary>
    public static void RenderRow(Team team, bool isActive)
    {
        var active = isActive ? "[green]▶[/] " : "  ";
        var legal  = team.Members.Count > 0 && team.Members.All(m => m.IsLegal != false)
            ? "[grey]✓[/]"
            : "[yellow]⚠[/]";
        var memberCount = $"[grey]{team.Members.Count}/6[/]";
        AnsiConsole.MarkupLine($"{active}[bold]{Markup.Escape(team.Name)}[/]  {memberCount}  {legal}");
    }

    /// <summary>Renders a full team detail: all six members with moves, items, and legality.</summary>
    public static void Render(Team team, LegalityReport? report, string? activeTeamName)
    {
        bool isActive = string.Equals(team.Name, activeTeamName, StringComparison.OrdinalIgnoreCase);

        AnsiConsole.MarkupLine(isActive
            ? $"[green]▶[/] [bold]{Markup.Escape(team.Name)}[/] [green](active)[/]"
            : $"  [bold]{Markup.Escape(team.Name)}[/]");

        if (team.FormatShowdownId is not null)
            AnsiConsole.MarkupLine($"  [grey]Format:[/] {Markup.Escape(team.FormatShowdownId)}");

        AnsiConsole.WriteLine();

        foreach (var member in team.Members)
            RenderMember(member, report);

        if (report is not null)
            RenderReport(report);

        AnsiConsole.WriteLine();
    }

    private static void RenderMember(TeamMember member, LegalityReport? report)
    {
        var speciesDisplay = FormatShowdownId(member.PokemonShowdownId ?? "???");
        var displayName = string.IsNullOrWhiteSpace(member.Nickname)
            ? speciesDisplay
            : $"{Markup.Escape(member.Nickname)} ({speciesDisplay})";

        // Legality indicator for this member
        var memberViolations = report?.Violations
            .Where(v => v.SlotIndex == member.SlotIndex)
            .ToList() ?? [];

        var legalMark = member.IsLegal == false || memberViolations.Count > 0
            ? "[yellow]⚠[/] "
            : member.IsLegal == true ? "[grey]  [/]" : "  ";

        AnsiConsole.MarkupLine($"  {legalMark}[bold]{displayName}[/]" +
            (member.Item   is not null ? $" @ {Markup.Escape(member.Item)}" : "") +
            (member.Nature is not null ? $"  [grey]{Markup.Escape(member.Nature)} Nature[/]" : ""));

        if (member.Ability is not null)
            AnsiConsole.MarkupLine($"       [grey]Ability:[/] {Markup.Escape(member.Ability)}");

        var moves = member.GetMoves().ToList();
        if (moves.Count > 0)
            AnsiConsole.MarkupLine("       [grey]Moves:[/]  " + string.Join("  /  ", moves.Select(Markup.Escape)));

        // Stat points summary (only non-zero)
        var sp = member.StatPoints;
        var spParts = new List<string>();
        if (sp.Hp  > 0) spParts.Add($"HP {sp.Hp}");
        if (sp.Atk > 0) spParts.Add($"Atk {sp.Atk}");
        if (sp.Def > 0) spParts.Add($"Def {sp.Def}");
        if (sp.SpA > 0) spParts.Add($"SpA {sp.SpA}");
        if (sp.SpD > 0) spParts.Add($"SpD {sp.SpD}");
        if (sp.Spe > 0) spParts.Add($"Spe {sp.Spe}");
        if (spParts.Count > 0)
            AnsiConsole.MarkupLine("       [grey]SPs:[/]    " + string.Join(" / ", spParts));

        AnsiConsole.WriteLine();
    }

    private static void RenderReport(LegalityReport report)
    {
        if (report.IsLegal && report.Warnings.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✓ Team is legal.[/]");
            return;
        }

        if (!report.IsLegal)
        {
            AnsiConsole.MarkupLine($"[red]✗ {report.Violations.Count} legality violation(s):[/]");
            foreach (var v in report.Violations)
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(v.Message)}");
        }

        if (report.Warnings.Count > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{report.Warnings.Count} warning(s):[/]");
            foreach (var w in report.Warnings)
                AnsiConsole.MarkupLine($"  [yellow]•[/] {Markup.Escape(w.Message)}");
        }
    }

    /// <summary>Converts a Showdown ID to a display name: "charizard-mega-y" → "Charizard Mega Y".</summary>
    private static string FormatShowdownId(string showdownId) =>
        string.Join(" ",
            showdownId.Split('-')
                .Select(part => part.Length > 0
                    ? char.ToUpperInvariant(part[0]) + part[1..]
                    : part));
}
