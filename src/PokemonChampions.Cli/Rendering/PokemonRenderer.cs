using PokemonChampions.Core.Calculation;
using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Enums;
using PokemonChampions.Shared.Extensions;
using Spectre.Console;

namespace PokemonChampions.Cli.Rendering;

public static class PokemonRenderer
{
    private static readonly StatName[] StatOrder =
        [StatName.Hp, StatName.Atk, StatName.Def, StatName.SpA, StatName.SpD, StatName.Spe];

    public static void Render(Pokemon pokemon)
    {
        // ── Header ──────────────────────────────────────────────────────────
        var typeStr = TypeColors.Badge(pokemon.Type1);
        if (pokemon.Type2.HasValue && pokemon.Type2 != PokemonType.None)
            typeStr += " " + TypeColors.Badge(pokemon.Type2.Value);

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(pokemon.Name)}[/]  {typeStr}");

        if (!pokemon.IsLegalInCurrentFormat)
            AnsiConsole.MarkupLine("[yellow]  ⚠ Not legal in the current format[/]");

        AnsiConsole.WriteLine();

        // ── Stat table ───────────────────────────────────────────────────────
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn(new TableColumn("[grey]Stat[/]"))
            .AddColumn(new TableColumn("[grey]Base[/]").RightAligned())
            .AddColumn(new TableColumn("[grey]Min[/]").RightAligned())
            .AddColumn(new TableColumn("[grey]Max[/]").RightAligned());

        foreach (var stat in StatOrder)
        {
            var range = StatCalculator.GetRange(pokemon.BaseStats.Get(stat), stat);
            table.AddRow(
                stat.DisplayName(),
                range.Base.ToString(),
                range.Min.ToString(),
                range.Max.ToString());
        }

        var bst = pokemon.BaseStats.Total;
        table.AddRow("[grey]BST[/]", $"[grey]{bst}[/]", "", "");

        AnsiConsole.Write(table);

        // ── Abilities ────────────────────────────────────────────────────────
        var abilities = pokemon.GetAbilities().ToList();
        if (abilities.Count > 0)
        {
            AnsiConsole.MarkupLine("[grey]Abilities:[/] " +
                string.Join("  |  ", abilities.Select(a => Markup.Escape(a))));
        }

        if (pokemon.IsMega && pokemon.BaseFormShowdownId is not null)
            AnsiConsole.MarkupLine($"[grey]Mega evolution of:[/] {Markup.Escape(pokemon.BaseFormShowdownId)}");

        AnsiConsole.WriteLine();
    }

    public static void RenderStatRange(Pokemon pokemon, StatName stat)
    {
        var range = StatCalculator.GetRange(pokemon.BaseStats.Get(stat), stat);
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(pokemon.Name)}[/] — {stat.DisplayName()}");
        AnsiConsole.MarkupLine(
            $"  Base [grey]→[/] [bold]{range.Base}[/]   " +
            $"Min [grey]→[/] [bold]{range.Min}[/]   " +
            $"Max [grey]→[/] [bold]{range.Max}[/]");
        AnsiConsole.WriteLine();
    }
}
