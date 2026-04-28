using PokemonChampions.Cli.Parsing;
using PokemonChampions.Cli.Rendering;
using PokemonChampions.Core.Calculation;
using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Services;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Enums;
using PokemonChampions.Shared.Extensions;
using Spectre.Console;

namespace PokemonChampions.Cli.Commands;

/// <summary>
/// Parses and dispatches a single line of user input from the REPL.
/// All public methods are thread-safe for calls from the REPL loop.
/// </summary>
public class CommandDispatcher(
    UpdateCommand updateCommand,
    ConfigCommand configCommand,
    TeamsCommand teamsCommand,
    MultiWordNameParser nameParser,
    IAliasService aliasService,
    ITeamService teamService,
    IPokemonService pokemonService,
    IUsageStatsService usageStatsService,
    ISettingsService settingsService)
{
    public const string HelpText = """
        [bold]Commands[/]
          [bold]update[/] [[--online]]            Refresh local data from Pokemon Showdown
          [bold]config[/] [[options]]             Show or change settings (--online/--offline/--format <id>)
          [bold]<name>[/]                         Look up a Pokemon, move, item, or ability
          [bold]<pokemon> <stat>[/]               Stat tier list vs. team (e.g. "incineroar speed")
          [bold]<pokemon> <stat> [[nature]] [[pts]][/]   Build comparison (e.g. "incineroar speed jolly 16")
          [bold]                 [[modifiers...]][/]      Modifiers: scarf, tailwind, para, +N, -N
          [bold]alias <text> <target>[/]          Create an alias  (e.g. alias mcy charizard-mega-y)
          [bold]alias remove <text>[/]            Remove an alias
          [bold]alias list[/]                     List all aliases
          [bold]teams[/]                          List saved teams
          [bold]teams new[/] [[--name <n>]] [[--file <path>]]   Import a team from pokepaste
          [bold]teams edit[/] <name>              Replace a team's pokepaste
          [bold]teams set[/] <name>               Set the active team
          [bold]teams remove[/] <name>            Delete a team
          [bold]teams validate[/] <name>          Re-validate a team against the current format
          [bold]help[/]                           Show this help message
          [bold]exit[/] / [bold]quit[/]                   Exit the program
        """;

    public async Task<bool> DispatchAsync(string input, CancellationToken ct)
    {
        input = input.Trim();
        if (string.IsNullOrEmpty(input)) return true;

        var tokens = Tokenize(input);
        if (tokens.Length == 0) return true;

        var verb = tokens[0].ToLowerInvariant();

        switch (verb)
        {
            case "exit":
            case "quit":
            case "q":
                return false; // signal REPL to stop

            case "help":
            case "?":
                AnsiConsole.MarkupLine(HelpText);
                return true;

            case "update":
                await updateCommand.RunFromReplAsync(tokens[1..], ct);
                return true;

            case "config":
                await configCommand.RunAsync(tokens[1..], ct);
                return true;

            case "alias":
                await HandleAliasAsync(tokens[1..], ct);
                return true;

            case "teams":
                await teamsCommand.RunAsync(tokens[1..], ct);
                return true;

            default:
                await HandleLookupAsync(tokens, ct);
                return true;
        }
    }

    // ── Alias command ─────────────────────────────────────────────────────────

    private async Task HandleAliasAsync(string[] args, CancellationToken ct)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("Usage: alias <text> <target> | alias remove <text> | alias list");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                await ListAliasesAsync(ct);
                return;

            case "remove":
                if (args.Length < 2) { AnsiConsole.MarkupLine("[red]Usage:[/] alias remove <text>"); return; }
                var aliasToRemove = string.Join(" ", args[1..]);
                if (await aliasService.DeleteAsync(aliasToRemove, ct))
                    AnsiConsole.MarkupLine($"[green]Removed alias:[/] {Markup.Escape(aliasToRemove)}");
                else
                    AnsiConsole.MarkupLine($"[red]Alias not found:[/] {Markup.Escape(aliasToRemove)}");
                return;

            default:
                // alias <text> <target...>
                // The alias text is args[0], target is the rest
                if (args.Length < 2)
                {
                    AnsiConsole.MarkupLine("[red]Usage:[/] alias <text> <target>");
                    return;
                }
                await CreateAliasAsync(args[0], args[1..], ct);
                return;
        }
    }

    private async Task CreateAliasAsync(string aliasText, string[] targetTokens, CancellationToken ct)
    {
        var matches = await nameParser.ResolveAsync(targetTokens, 0, ct);
        if (matches.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]Target not found:[/] {Markup.Escape(string.Join(" ", targetTokens))}");
            return;
        }

        var match = matches.Count == 1
            ? matches[0]
            : await DisambiguateAsync(matches, ct);

        if (match is null) return;

        var (entityType, showdownId) = GetEntityTypeAndShowdownId(match.Entity);
        await aliasService.CreateAsync(aliasText, entityType, showdownId, ct);
        AnsiConsole.MarkupLine($"[green]Alias created:[/] {Markup.Escape(aliasText)} → {Markup.Escape(GetEntityName(match.Entity))}");
    }

    private async Task ListAliasesAsync(CancellationToken ct)
    {
        var aliases = await aliasService.GetAllAsync(ct);
        if (aliases.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No aliases defined.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Alias")
            .AddColumn("Type")
            .AddColumn("Target ID");

        foreach (var (text, type, id) in aliases)
            table.AddRow(Markup.Escape(text), type.ToString(), id.ToString());

        AnsiConsole.Write(table);
    }

    // ── Lookup command ────────────────────────────────────────────────────────

    private async Task HandleLookupAsync(string[] tokens, CancellationToken ct)
    {
        // Check for stat lookup: "<pokemon...> <stat> [modifiers...]"
        // Scan forward from index 1 for the first stat token; everything after it is modifiers.
        if (tokens.Length >= 2)
        {
            for (int si = 1; si < tokens.Length; si++)
            {
                if (!StatExtensions.TryParseStatFromTokens(tokens, si, out var stat, out var consumed)) continue;
                var pokemonTokens = tokens[..si];
                var modifierTokens = tokens[(si + consumed)..];
                var pokemon = await ResolvePokemonAsync(pokemonTokens, ct);
                if (pokemon is not null)
                {
                    var build = ParseBuildFromTokens(modifierTokens);
                    var modifiers = StatModifierSet.Parse(modifierTokens);
                    if (build.HasAny)
                        await RenderStatBuildAsync(pokemon, stat, build, modifiers, ct);
                    else
                        await RenderStatTierAsync(pokemon, stat, modifiers, ct);
                    return;
                }
            }
        }

        // Check for --detailed flag
        var detailed = tokens.Any(t => t.Equals("--detailed", StringComparison.OrdinalIgnoreCase));
        var lookupTokens = detailed
            ? tokens.Where(t => !t.Equals("--detailed", StringComparison.OrdinalIgnoreCase)).ToArray()
            : tokens;

        // General entity lookup
        var matches = await nameParser.ResolveAsync(lookupTokens, 0, ct);

        if (matches.Count == 0)
        {
            // Offer fuzzy suggestions
            var query = string.Join(" ", lookupTokens);
            var fuzzy = await nameParser.FuzzySearchAsync(query, maxPerType: 3, ct);
            AnsiConsole.MarkupLine($"[red]Not found:[/] {Markup.Escape(query)}");
            if (fuzzy.Count > 0)
            {
                AnsiConsole.MarkupLine("[grey]Did you mean:[/]");
                foreach (var (type, name, id) in fuzzy.Take(5))
                    AnsiConsole.MarkupLine($"  {Markup.Escape(name)} [grey]({type.ToString().ToLower()})[/]");
            }
            return;
        }

        var result = matches.Count == 1
            ? matches[0]
            : await DisambiguateAsync(matches, ct);

        if (result is null) return;

        if (result.Entity is Pokemon foundPokemon)
        {
            var activeMember = await GetActiveTeamMemberAsync(foundPokemon.ShowdownId, ct);
            var formatId = await settingsService.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct)
                           ?? AppConstants.DefaultFormat;
            var usageStats = await usageStatsService.GetAsync(foundPokemon.ShowdownId, formatId, ct);
            PokemonRenderer.Render(foundPokemon, activeMember, usageStats, detailed);
        }
        else
        {
            RenderEntity(result.Entity);
        }
    }

    private async Task<TeamMember?> GetActiveTeamMemberAsync(string pokemonShowdownId, CancellationToken ct)
    {
        var team = await teamService.GetActiveAsync(ct);
        return team?.Members.FirstOrDefault(m =>
            string.Equals(m.PokemonShowdownId, pokemonShowdownId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Pokemon?> ResolvePokemonAsync(string[] tokens, CancellationToken ct)
    {
        if (tokens.Length == 0) return null;
        var matches = await nameParser.ResolveAsync(tokens, 0, ct);
        var pokemonMatch = matches.FirstOrDefault(m => m.Type == EntityType.Pokemon);
        return pokemonMatch?.Entity as Pokemon;
    }

    private async Task RenderStatTierAsync(Pokemon pokemon, StatName stat, StatModifierSet modifiers, CancellationToken ct)
    {
        var entries = await GetTeamEntriesAsync(stat, ct);
        PokemonRenderer.RenderStatTier(pokemon, stat, entries, modifiers);
    }

    private async Task RenderStatBuildAsync(
        Pokemon pokemon, StatName stat, BuildSpec build, StatModifierSet modifiers, CancellationToken ct)
    {
        int baseStat = pokemon.BaseStats.Get(stat);
        int evOrSp = build.EvOrSp ?? 0;
        int rawEv = evOrSp <= AppConstants.MaxStatPointsPerStat ? evOrSp * 8 : evOrSp;
        double natureMult = stat != StatName.Hp ? (build.Nature?.GetMultiplier(stat) ?? 1.0) : 1.0;
        int unmodified = StatCalculator.Calculate(stat, baseStat, AppConstants.MaxIv, rawEv, natureMult);

        var entries = await GetTeamEntriesAsync(stat, ct);
        PokemonRenderer.RenderStatBuild(
            pokemon, stat, modifiers.Apply(unmodified), unmodified, BuildLabelFor(build, stat), entries, modifiers);
    }

    private async Task<List<StatTierEntry>> GetTeamEntriesAsync(StatName stat, CancellationToken ct)
    {
        var entries = new List<StatTierEntry>();
        var team = await teamService.GetActiveAsync(ct);
        if (team is null) return entries;

        foreach (var member in team.Members)
        {
            if (member.PokemonShowdownId is null) continue;
            var species = await pokemonService.FindAsync(member.PokemonShowdownId, ct);
            if (species is null) continue;

            var range = StatCalculator.GetRange(species.BaseStats.Get(stat), stat);
            int? actual = null;
            if (member.Nature is not null)
            {
                var nature = Nature.TryGet(member.Nature) ?? new Nature("?", null, null);
                actual = StatCalculator.Compute(species.BaseStats, member.StatPoints, member.Ivs, nature).Get(stat);
            }

            entries.Add(new StatTierEntry(member.DisplayName(species.Name), range.Min, range.Max, actual));
        }

        return entries;
    }

    // ── Build parsing ─────────────────────────────────────────────────────────

    private record BuildSpec(Nature? Nature, int? EvOrSp)
    {
        public bool HasAny => Nature is not null || EvOrSp.HasValue;
    }

    private static BuildSpec ParseBuildFromTokens(string[] tokens)
    {
        Nature? nature = null;
        int? evOrSp = null;
        foreach (var raw in tokens)
        {
            var t = raw.ToLowerInvariant().Trim();
            if (nature is null && Nature.TryGet(t) is { } n) { nature = n; continue; }
            if (evOrSp is null && !t.StartsWith('+') && !t.StartsWith('-')
                && int.TryParse(t, out int val) && val >= 0)
                evOrSp = val;
        }
        return new BuildSpec(nature, evOrSp);
    }

    private static string BuildLabelFor(BuildSpec build, StatName stat)
    {
        var parts = new List<string>();
        if (build.Nature is not null && stat != StatName.Hp)
            parts.Add(build.Nature.Name);
        if (build.EvOrSp.HasValue)
        {
            int v = build.EvOrSp.Value;
            parts.Add(v <= AppConstants.MaxStatPointsPerStat ? $"{v} SP" : $"{v} EVs");
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "Neutral";
    }

    // ── Disambiguation ────────────────────────────────────────────────────────

    private static Task<MultiWordNameParser.LookupResult?> DisambiguateAsync(
        IReadOnlyList<MultiWordNameParser.LookupResult> matches,
        CancellationToken ct)
    {
        var options = matches
            .Select(m => $"{GetEntityName(m.Entity)} ({m.Type.ToString().ToLower()})")
            .ToList();

        AnsiConsole.MarkupLine("[yellow]Ambiguous name — did you mean:[/]");
        for (int i = 0; i < options.Count; i++)
            AnsiConsole.MarkupLine($"  [[{i + 1}]] {Markup.Escape(options[i])}");

        AnsiConsole.Markup("Enter number: ");
        var line = Console.ReadLine()?.Trim();
        if (int.TryParse(line, out var choice) && choice >= 1 && choice <= matches.Count)
            return Task.FromResult<MultiWordNameParser.LookupResult?>(matches[choice - 1]);

        AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
        return Task.FromResult<MultiWordNameParser.LookupResult?>(null);
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    private static void RenderEntity(object entity)
    {
        switch (entity)
        {
            case Pokemon p:   PokemonRenderer.Render(p); break;
            case Move m:      MoveRenderer.Render(m); break;
            case Item i:      ItemRenderer.Render(i); break;
            case Ability a:   AbilityRenderer.Render(a); break;
            default:          AnsiConsole.MarkupLine("[grey](unknown entity type)[/]"); break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetEntityName(object entity) => entity switch
    {
        Pokemon p  => p.Name,
        Move m     => m.Name,
        Item i     => i.Name,
        Ability a  => a.Name,
        _          => entity.ToString() ?? "?"
    };

    private static (EntityType, string) GetEntityTypeAndShowdownId(object entity) => entity switch
    {
        Pokemon p => (EntityType.Pokemon, p.ShowdownId),
        Move m    => (EntityType.Move, m.ShowdownId),
        Item i    => (EntityType.Item, i.ShowdownId),
        Ability a => (EntityType.Ability, a.ShowdownId),
        _         => (EntityType.Pokemon, string.Empty)
    };

    // ── Tokenizer ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Splits input on whitespace, respecting double-quoted strings.
    /// e.g. alias "mega charizard y" charizard-mega-y → ["alias", "mega charizard y", "charizard-mega-y"]
    /// </summary>
    public static string[] Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return [.. tokens];
    }
}
