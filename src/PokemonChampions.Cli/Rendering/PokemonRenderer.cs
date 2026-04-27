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

    public static void Render(Pokemon pokemon, TeamMember? member = null)
    {
        // ── Header ──────────────────────────────────────────────────────────
        var typeStr = TypeColors.Badge(pokemon.Type1);
        if (pokemon.Type2.HasValue && pokemon.Type2 != PokemonType.None)
            typeStr += " " + TypeColors.Badge(pokemon.Type2.Value);

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(pokemon.Name)}[/]  {typeStr}");

        if (!pokemon.IsLegalInCurrentFormat)
            AnsiConsole.MarkupLine("[yellow]  ⚠ Not legal in the current format[/]");

        AnsiConsole.WriteLine();

        // ── Base stat table ──────────────────────────────────────────────────
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

        // ── Team build ───────────────────────────────────────────────────────
        if (member is not null)
            RenderBuild(pokemon, member);
        else
            AnsiConsole.WriteLine();
    }

    public static void RenderStatTier(Pokemon pokemon, StatName stat, IReadOnlyList<StatTierEntry> teamEntries)
    {
        var range = StatCalculator.GetRange(pokemon.BaseStats.Get(stat), stat);

        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(pokemon.Name)}[/] — {stat.DisplayName()}  " +
            $"[grey]Base {range.Base}   Min {range.Min}   Max {range.Max}[/]");
        AnsiConsole.WriteLine();

        var minRows = BuildTierRows(pokemon.Name, range.Min, teamEntries, useMax: false)
            .OrderByDescending(r => r.Stat).ToList();
        AnsiConsole.MarkupLine("[grey]── Uninvested[/]");
        RenderTierTable(minRows);
        AnsiConsole.WriteLine();

        var maxRows = BuildTierRows(pokemon.Name, range.Max, teamEntries, useMax: true)
            .OrderByDescending(r => r.Stat).ToList();
        AnsiConsole.MarkupLine("[grey]── Max invest[/]");
        RenderTierTable(maxRows);
        AnsiConsole.WriteLine();
    }

    private record TierRow(string Name, int Stat, bool IsQueried, bool IsEstimated);

    private static List<TierRow> BuildTierRows(
        string queriedName, int queriedStat,
        IReadOnlyList<StatTierEntry> teamEntries, bool useMax)
    {
        var rows = new List<TierRow>
        {
            new(queriedName, queriedStat, IsQueried: true, IsEstimated: false)
        };

        foreach (var entry in teamEntries)
        {
            bool estimated = !entry.Actual.HasValue;
            int stat = entry.Actual ?? (useMax ? entry.Max : entry.Min);
            rows.Add(new TierRow(entry.Name, stat, IsQueried: false, IsEstimated: estimated));
        }

        return rows;
    }

    private static void RenderTierTable(IReadOnlyList<TierRow> rows)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("").RightAligned().Width(5))
            .AddColumn(new TableColumn("").LeftAligned());

        foreach (var row in rows)
        {
            string statMarkup = row.IsQueried ? $"[bold]{row.Stat}[/]" : row.Stat.ToString();
            string nameMarkup = row.IsQueried
                ? $"[bold]{Markup.Escape(row.Name)}[/]"
                : row.IsEstimated
                    ? $"{Markup.Escape(row.Name)}  [grey](team, est.)[/]"
                    : $"{Markup.Escape(row.Name)}  [grey](team)[/]";

            table.AddRow(statMarkup, nameMarkup);
        }

        AnsiConsole.Write(table);
    }

    private static void RenderBuild(Pokemon pokemon, TeamMember member)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]── Your build ─────────────────────────────────────[/]");

        // Nature + Item + Ability header line
        var nature = member.Nature is not null ? Nature.TryGet(member.Nature) : null;
        var natureName = member.Nature ?? "[grey]No nature[/]";
        var itemPart = member.Item is not null ? $"  [grey]@[/] {Markup.Escape(member.Item)}" : "";
        AnsiConsole.MarkupLine($"  {Markup.Escape(natureName)}{itemPart}");

        if (member.Ability is not null)
            AnsiConsole.MarkupLine($"  [grey]Ability:[/] {Markup.Escape(member.Ability)}");

        AnsiConsole.WriteLine();

        // Computed stat table
        var effectiveNature = nature ?? new Nature("?", null, null);
        var computed = StatCalculator.Compute(pokemon.BaseStats, member.StatPoints, member.Ivs, effectiveNature);

        var buildTable = new Table()
            .Border(TableBorder.Simple)
            .HideHeaders()
            .AddColumn(new TableColumn("").LeftAligned())
            .AddColumn(new TableColumn("").RightAligned())  // Pts
            .AddColumn(new TableColumn("").RightAligned()); // Final

        buildTable.AddRow("[grey]Stat[/]", "[grey]Pts[/]", "[grey]Final[/]");

        foreach (var stat in StatOrder)
        {
            var pts = member.StatPoints.Get(stat);
            var final = computed.Get(stat);
            bool isBoosted  = stat != StatName.Hp && effectiveNature.BoostedStat  == stat;
            bool isHindered = stat != StatName.Hp && effectiveNature.HinderedStat == stat;

            string finalMarkup = isBoosted  ? $"[green]{final}[/]"
                               : isHindered ? $"[red]{final}[/]"
                               : final.ToString();

            string ptsStr = pts <= 0 ? "[grey]—[/]"
                          : isBoosted  ? $"[green]{pts}+[/]"
                          : isHindered ? $"[red]{pts}-[/]"
                          : pts.ToString();

            buildTable.AddRow(stat.Abbreviation(), ptsStr, finalMarkup);
        }

        AnsiConsole.Write(buildTable);

        // Moves
        var moves = member.GetMoves().ToList();
        if (moves.Count > 0)
            AnsiConsole.MarkupLine("  [grey]Moves:[/]  " + string.Join("  [grey]/[/]  ", moves.Select(Markup.Escape)));

        AnsiConsole.WriteLine();
    }
}
