using PokemonChampions.Core.Calculation;
using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Enums;
using Spectre.Console;

namespace PokemonChampions.Cli.Rendering;

public static class DamageRenderer
{
    /// <summary>
    /// Renders one or two damage calc scenarios.
    /// Pass a single result (defenderScenarios.Count == 1) when the defender is a known team member,
    /// or two results (min bulk / max bulk) for an unknown opponent.
    /// </summary>
    public static void Render(
        Move move,
        Pokemon attacker,
        string attackerLabel,
        Pokemon defender,
        IReadOnlyList<(string Label, DamageResult Result)> scenarios,
        BattleState battle,
        bool isIncoming)
    {
        // ── Header ────────────────────────────────────────────────────────────
        var moveTypeStr  = TypeColors.Badge(move.Type);
        var categoryStr  = $"[grey]{move.Category}[/]";
        var powerStr     = move.Power.HasValue ? $"[grey]{move.Power} BP[/]" : "[grey]— BP[/]";
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(move.Name)}[/]  {moveTypeStr}  {categoryStr}  {powerStr}");

        // ── Attacker / Defender line ──────────────────────────────────────────
        var atkPart = $"[bold]{Markup.Escape(attacker.Name)}[/] [grey]({Markup.Escape(attackerLabel)})[/]";
        var defPart = $"[bold]{Markup.Escape(defender.Name)}[/]";
        if (isIncoming)
            AnsiConsole.MarkupLine($"  {defPart}  [grey]receives from[/]  {atkPart}");
        else
            AnsiConsole.MarkupLine($"  {atkPart}  [grey]→[/]  {defPart}");

        // ── Effect notes (STAB, type effectiveness) ───────────────────────────
        if (scenarios.Count > 0)
        {
            var first = scenarios[0].Result;
            var notes = new List<string>();
            if (first.IsStab) notes.Add("[bold]STAB[/]");
            notes.Add(EffectivenessLabel(first.TypeEffectiveness));
            AnsiConsole.MarkupLine("  " + string.Join("  [grey]|[/]  ", notes));
        }

        if (battle.Weather != DamageWeather.None)
            AnsiConsole.MarkupLine($"  [grey]Weather: {battle.Weather}[/]");
        if (battle.Screens)
            AnsiConsole.MarkupLine("  [grey]Screens active[/]");

        AnsiConsole.WriteLine();

        // ── Damage rows ───────────────────────────────────────────────────────
        if (scenarios.Count > 0 && scenarios[0].Result.TypeEffectiveness == 0)
        {
            AnsiConsole.MarkupLine("[yellow]  No effect — immune[/]");
            AnsiConsole.WriteLine();
            return;
        }

        if (scenarios.Count > 0 && scenarios[0].Result.MaxDamage == 0 && move.Category == MoveCategory.Status)
        {
            AnsiConsole.MarkupLine("[grey]  Status move — no direct damage[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").LeftAligned().Width(32))
            .AddColumn(new TableColumn("").LeftAligned().Width(16))
            .AddColumn(new TableColumn("").LeftAligned());

        foreach (var (label, result) in scenarios)
        {
            string dmgStr  = $"[bold]{result.MinDamage}–{result.MaxDamage}[/]";
            string pctStr  = $"[grey]({result.MinPct:F1}%–{result.MaxPct:F1}%)[/]";
            string koNote  = KoNote(result);
            table.AddRow(
                Markup.Escape(label),
                dmgStr + "  " + pctStr,
                koNote);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static string EffectivenessLabel(double eff) => eff switch
    {
        0   => "[red]No effect[/]",
        0.25=> "[red]0.25× (very resisted)[/]",
        0.5 => "[yellow]0.5× (not very effective)[/]",
        1.0 => "[grey]Neutral[/]",
        2.0 => "[green]2× (super effective)[/]",
        4.0 => "[bold green]4× (doubly super effective)[/]",
        _   => $"[grey]{eff:F2}×[/]"
    };

    private static string KoNote(DamageResult r)
    {
        if (r.DefenderHp <= 0) return string.Empty;
        if (r.MinDamage >= r.DefenderHp) return "[bold red]guaranteed OHKO[/]";
        if (r.MaxDamage >= r.DefenderHp) return "[yellow]possible OHKO[/]";
        return string.Empty;
    }
}
