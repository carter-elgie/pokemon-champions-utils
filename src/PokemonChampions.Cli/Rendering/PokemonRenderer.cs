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

    public static void Render(
        Pokemon pokemon,
        TeamMember? member = null,
        PokemonUsageStats? usage = null,
        bool detailed = false)
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
            .AddColumn(new TableColumn("[grey]Range[/]").RightAligned());

        foreach (var stat in StatOrder)
        {
            var range = StatCalculator.GetRange(pokemon.BaseStats.Get(stat), stat);
            table.AddRow(
                stat.DisplayName(),
                range.Base.ToString(),
                FormatRange(range));
        }

        var bst = pokemon.BaseStats.Total;
        table.AddRow("[grey]BST[/]", $"[grey]{bst}[/]", "");

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

        // ── Usage stats ──────────────────────────────────────────────────────
        if (usage is not null)
            RenderUsage(usage, detailed);
        else
            AnsiConsole.WriteLine();

        // ── Team build ───────────────────────────────────────────────────────
        if (member is not null)
            RenderBuild(pokemon, member);
        else if (usage is null)
            AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Formats a stat range as "min-softMin-softMax-max", with the true min/max
    /// (0 pts + hindering / max pts + boosting) bolded to stand out from the
    /// "soft" values (0 pts / max pts with a neutral nature for that stat).
    /// </summary>
    private static string FormatRange(StatRange range) =>
        FormatRange(range.Min, range.SoftMin, range.SoftMax, range.Max);

    private static string FormatRange(int min, int softMin, int softMax, int max) =>
        $"[bold cyan]{min}[/]-{softMin}-{softMax}-[bold cyan]{max}[/]";

    private static void RenderUsage(PokemonUsageStats usage, bool detailed)
    {
        AnsiConsole.WriteLine();
        var monthLabel = usage.StatsMonth is not null ? $" ({usage.StatsMonth})" : string.Empty;
        AnsiConsole.MarkupLine($"[grey]── Usage stats{Markup.Escape(monthLabel)} ─────────────────────────────────[/]");
        AnsiConsole.MarkupLine($"  Usage: [bold]{usage.UsagePct:F2}%[/]");

        if (usage.Moves.Count > 0)
        {
            int showCount = detailed ? usage.Moves.Count : Math.Min(5, usage.Moves.Count);
            var moveParts = usage.Moves.Take(showCount)
                .Select(m => $"{Markup.Escape(m.Name)} [grey]({m.UsagePct:F1}%)[/]");
            AnsiConsole.MarkupLine("  [grey]Moves:[/]  " + string.Join("  [grey]|[/]  ", moveParts));
        }
        else
        {
            AnsiConsole.MarkupLine("  [grey]Move data not yet cached — run[/] update --online [grey]or look up again online.[/]");
        }

        if (usage.Teammates.Count > 0)
        {
            int showCount = detailed ? usage.Teammates.Count : Math.Min(3, usage.Teammates.Count);
            var tmParts = usage.Teammates.Take(showCount)
                .Select(t => $"{Markup.Escape(t.Name)} [grey]({t.UsagePct:F1}%)[/]");
            AnsiConsole.MarkupLine("  [grey]Teammates:[/]  " + string.Join("  [grey]|[/]  ", tmParts));
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders the stat lookup: the queried Pokemon's min-softMin-softMax-max range at the
    /// top, followed by a single list of the active team's stats for the same category.
    /// If the queried Pokemon is on the active team with a build, <paramref name="teamBuildStat"/>
    /// is shown as a highlighted "Your build" line above the team list.
    /// </summary>
    public static void RenderStatTier(
        Pokemon pokemon, StatName stat,
        IReadOnlyList<StatTierEntry> teamEntries,
        StatModifierSet? modifiers = null,
        int? teamBuildStat = null,
        string? teamBuildLabel = null)
    {
        modifiers ??= StatModifierSet.None;
        var range = StatCalculator.GetRange(pokemon.BaseStats.Get(stat), stat);

        // Header: name, stat name, base, and the four-value range
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(pokemon.Name)}[/] — {stat.DisplayName()}  " +
            $"[grey]Base {range.Base}[/]   {FormatRange(range)}");

        // Team build line (when the queried Pokemon is on the active team)
        if (teamBuildStat.HasValue)
        {
            var teamNote = teamBuildLabel is not null ? $"  [grey]({Markup.Escape(teamBuildLabel)})[/]" : string.Empty;
            AnsiConsole.MarkupLine($"  [grey]Your build:[/] [bold]{teamBuildStat.Value}[/]{teamNote}");
        }

        // Modifier line (only shown when active for this stat)
        if (modifiers.HasAny(stat, pokemon))
        {
            var mult = modifiers.TotalMultiplier(stat, pokemon);
            var modRange = FormatRange(
                modifiers.Apply(range.Min, stat, pokemon),
                modifiers.Apply(range.SoftMin, stat, pokemon),
                modifiers.Apply(range.SoftMax, stat, pokemon),
                modifiers.Apply(range.Max, stat, pokemon));
            AnsiConsole.MarkupLine(
                $"  [grey]{Markup.Escape(modifiers.Describe(stat, pokemon))} (×{mult:F2})[/]   " +
                $"[grey]→[/]  {modRange}");
        }

        AnsiConsole.WriteLine();

        if (teamEntries.Count > 0)
        {
            AnsiConsole.MarkupLine("[grey]── Your team[/]");
            var rows = teamEntries
                .Select(e => new TierRow(e.Name, e.Actual ?? e.Max, IsQueried: false, IsEstimated: !e.Actual.HasValue))
                .OrderByDescending(r => r.Stat)
                .ToList();
            RenderTierTable(rows);
            AnsiConsole.WriteLine();
        }
    }

    public static void RenderStatBuild(
        Pokemon pokemon,
        StatName stat,
        int queriedStat,
        int unmodifiedStat,
        string buildLabel,
        IReadOnlyList<StatTierEntry> teamEntries,
        StatModifierSet? modifiers = null)
    {
        modifiers ??= StatModifierSet.None;
        var range = StatCalculator.GetRange(pokemon.BaseStats.Get(stat), stat);

        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(pokemon.Name)}[/] — {stat.DisplayName()}  " +
            $"[grey]Base {range.Base}[/]   {FormatRange(range)}");

        AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(buildLabel)}[/] → [bold]{unmodifiedStat}[/]");

        if (modifiers.HasAny(stat, pokemon))
        {
            AnsiConsole.MarkupLine(
                $"  [grey]{Markup.Escape(modifiers.Describe(stat, pokemon))} (×{modifiers.TotalMultiplier(stat, pokemon):F2})[/]   " +
                $"[grey]→[/] [bold]{queriedStat}[/]");
        }

        AnsiConsole.WriteLine();

        var modLabel = modifiers.HasAny(stat, pokemon) ? $" + {modifiers.Describe(stat, pokemon)}" : string.Empty;
        AnsiConsole.MarkupLine($"[grey]── Build comparison{Markup.Escape(modLabel)}[/]");

        var rows = BuildTierRows(pokemon.Name, queriedStat, teamEntries)
            .OrderByDescending(r => r.Stat).ToList();
        RenderTierTable(rows);
        AnsiConsole.WriteLine();
    }

    private record TierRow(string Name, int Stat, bool IsQueried, bool IsEstimated);

    private static List<TierRow> BuildTierRows(
        string queriedName, int queriedStat, IReadOnlyList<StatTierEntry> teamEntries)
    {
        var rows = new List<TierRow>
        {
            new(queriedName, queriedStat, IsQueried: true, IsEstimated: false)
        };

        foreach (var entry in teamEntries)
        {
            bool estimated = !entry.Actual.HasValue;
            int stat = entry.Actual ?? entry.Min;
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
