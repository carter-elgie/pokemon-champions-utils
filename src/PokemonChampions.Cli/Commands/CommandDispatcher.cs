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
          [bold]<atk> [[+N]] <move> > <def> [[-N]][/]    Outgoing damage calc (e.g. "sneasler +1 close-combat > incineroar")
          [bold]<def> < <atk> [[+N]] <move>[/]           Incoming damage calc (e.g. "incineroar < sneasler close-combat")
          [bold]             [[--weather sun|rain|sand|snow]] [[--screens]][/]
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
        // Check for damage calc: tokens containing ">" or "<" operator
        int gtIdx = Array.IndexOf(tokens, ">");
        int ltIdx = Array.IndexOf(tokens, "<");
        if (gtIdx > 0)
        {
            await HandleDamageCalcAsync(tokens, gtIdx, isOutgoing: true, ct);
            return;
        }
        if (ltIdx > 0 && ltIdx < tokens.Length - 1)
        {
            await HandleDamageCalcAsync(tokens, ltIdx, isOutgoing: false, ct);
            return;
        }

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

    // ── Damage calculation ────────────────────────────────────────────────────

    /// <summary>
    /// Handles "attacker [+N] move > defender [-N] [--weather W] [--screens]"
    /// and the reversed incoming form "defender < attacker [+N] move [flags]".
    /// </summary>
    private async Task HandleDamageCalcAsync(string[] allTokens, int opIdx, bool isOutgoing, CancellationToken ct)
    {
        var battle = ExtractBattleState(allTokens, out var cleanTokens);
        opIdx = Array.IndexOf(cleanTokens, isOutgoing ? ">" : "<");
        if (opIdx < 0) { AnsiConsole.MarkupLine("[red]Could not parse calc expression.[/]"); return; }

        string[] atkSideTokens = isOutgoing ? cleanTokens[..opIdx] : cleanTokens[(opIdx + 1)..];
        string[] defSideTokens = isOutgoing ? cleanTokens[(opIdx + 1)..] : cleanTokens[..opIdx];

        var (attacker, move, atkStage) = await ParseAttackerSideAsync(atkSideTokens, ct);
        var (defender, defStage)       = await ParseDefenderSideAsync(defSideTokens, ct);

        if (attacker is null)
        { AnsiConsole.MarkupLine("[red]Could not resolve attacker.[/]"); return; }
        if (move is null)
        { AnsiConsole.MarkupLine("[red]Could not resolve move.[/]"); return; }
        if (defender is null)
        { AnsiConsole.MarkupLine("[red]Could not resolve defender.[/]"); return; }

        if (move.Power is null or 0 && move.Category != MoveCategory.Status)
        {
            AnsiConsole.MarkupLine("[grey]Power not available for this move — cannot calculate damage.[/]");
            return;
        }

        bool isPhysical = move.Category == MoveCategory.Physical;

        // ── Resolve attacker stats ─────────────────────────────────────────
        var activeTeam   = await teamService.GetActiveAsync(ct);
        var atkMember    = activeTeam?.Members.FirstOrDefault(m =>
            string.Equals(m.PokemonShowdownId, attacker.ShowdownId, StringComparison.OrdinalIgnoreCase));

        ComputedStats atkStats;
        string attackerLabel;
        string? atkAbility, atkItem;

        if (atkMember is not null && atkMember.Nature is not null)
        {
            var nature = Nature.TryGet(atkMember.Nature) ?? new Nature("?", null, null);
            atkStats   = StatCalculator.Compute(attacker.BaseStats, atkMember.StatPoints, atkMember.Ivs, nature);
            atkAbility = atkMember.Ability;
            atkItem    = atkMember.Item;
            attackerLabel = BuildAttackerLabel(atkMember, nature, isPhysical);
        }
        else
        {
            // Unknown build — show max offensive investment
            int maxEv = AppConstants.MaxStatPointsPerStat * 8;
            int maxAtk = StatCalculator.CalculateStat(attacker.BaseStats.Atk, AppConstants.MaxIv, maxEv, 1.1);
            int maxSpA = StatCalculator.CalculateStat(attacker.BaseStats.SpA, AppConstants.MaxIv, maxEv, 1.1);
            atkStats   = new ComputedStats(0, maxAtk, 0, maxSpA, 0, 0);
            atkAbility = null;
            atkItem    = null;
            attackerLabel = isPhysical ? $"max Atk ({maxAtk})" : $"max SpA ({maxSpA})";
        }

        // ── Resolve defender scenarios ─────────────────────────────────────
        var defMember = activeTeam?.Members.FirstOrDefault(m =>
            string.Equals(m.PokemonShowdownId, defender.ShowdownId, StringComparison.OrdinalIgnoreCase));

        var scenarios = new List<(string Label, DamageResult Result)>();

        if (defMember is not null && defMember.Nature is not null)
        {
            var defNature = Nature.TryGet(defMember.Nature) ?? new Nature("?", null, null);
            var defStats  = StatCalculator.Compute(defender.BaseStats, defMember.StatPoints, defMember.Ivs, defNature);
            var ctx = new DamageContext(attacker, atkStats, atkAbility, atkItem, atkStage,
                                        defender, defStats, defStage, move, battle);
            var result = DamageCalculator.Calculate(ctx);
            scenarios.Add(("Your " + defMember.DisplayName(defender.Name), result));
        }
        else
        {
            int maxEv = AppConstants.MaxStatPointsPerStat * 8;
            StatName defStatName = isPhysical ? StatName.Def : StatName.SpD;
            int baseDefStat = isPhysical ? defender.BaseStats.Def : defender.BaseStats.SpD;

            // Min bulk scenario
            int minDefStat = StatCalculator.CalculateStat(baseDefStat, AppConstants.MaxIv, 0, 0.9);
            int minHp      = StatCalculator.CalculateHp(defender.BaseStats.Hp, AppConstants.MaxIv, 0);
            var minStats   = isPhysical
                ? new ComputedStats(minHp, 0, minDefStat, 0, 0, 0)
                : new ComputedStats(minHp, 0, 0, 0, minDefStat, 0);
            var ctxMin     = new DamageContext(attacker, atkStats, atkAbility, atkItem, atkStage,
                                               defender, minStats, defStage, move, battle);
            scenarios.Add(($"0 {defStatName} ({minDefStat} / {minHp} HP)", DamageCalculator.Calculate(ctxMin)));

            // Max bulk scenario
            int maxDefStat = StatCalculator.CalculateStat(baseDefStat, AppConstants.MaxIv, maxEv, 1.1);
            int maxHp      = StatCalculator.CalculateHp(defender.BaseStats.Hp, AppConstants.MaxIv, maxEv);
            var maxStats   = isPhysical
                ? new ComputedStats(maxHp, 0, maxDefStat, 0, 0, 0)
                : new ComputedStats(maxHp, 0, 0, 0, maxDefStat, 0);
            var ctxMax     = new DamageContext(attacker, atkStats, atkAbility, atkItem, atkStage,
                                               defender, maxStats, defStage, move, battle);
            scenarios.Add(($"Max {defStatName} ({maxDefStat} / {maxHp} HP)", DamageCalculator.Calculate(ctxMax)));
        }

        DamageRenderer.Render(move, attacker, attackerLabel, defender, scenarios, battle, !isOutgoing);
    }

    private static string BuildAttackerLabel(TeamMember member, Nature nature, bool isPhysical)
    {
        var parts = new List<string>();
        if (!nature.IsNeutral) parts.Add(nature.Name);
        var sp = isPhysical ? member.StatPoints.Atk : member.StatPoints.SpA;
        if (sp > 0) parts.Add($"{sp} SP {(isPhysical ? "Atk" : "SpA")}");
        if (member.Item is not null) parts.Add($"@ {member.Item}");
        return parts.Count > 0 ? string.Join(", ", parts) : "team";
    }

    private async Task<(Pokemon? Pokemon, Move? Move, int Stage)> ParseAttackerSideAsync(
        string[] tokens, CancellationToken ct)
    {
        int stage = ExtractStage(tokens, out var rest);

        // Try all splits: first N tokens = pokemon, remainder = move
        for (int pokemonLen = 1; pokemonLen < rest.Length; pokemonLen++)
        {
            var pokemonTokens = rest[..pokemonLen];
            var moveTokens    = rest[pokemonLen..];
            if (moveTokens.Length == 0) continue;

            var pokemon = await ResolvePokemonAsync(pokemonTokens, ct);
            if (pokemon is null) continue;

            var move = await ResolveMoveAsync(moveTokens, ct);
            if (move is not null) return (pokemon, move, stage);
        }

        return (null, null, 0);
    }

    private async Task<(Pokemon? Pokemon, int Stage)> ParseDefenderSideAsync(
        string[] tokens, CancellationToken ct)
    {
        int stage = ExtractStage(tokens, out var rest);
        var pokemon = await ResolvePokemonAsync(rest, ct);
        return (pokemon, stage);
    }

    private static int ExtractStage(string[] tokens, out string[] remainder)
    {
        int stage = 0;
        var kept  = new List<string>();
        foreach (var t in tokens)
        {
            if (IsStageToken(t, out int s)) { stage = Math.Clamp(stage + s, -6, 6); }
            else kept.Add(t);
        }
        remainder = [.. kept];
        return stage;
    }

    private static bool IsStageToken(string token, out int stage)
    {
        stage = 0;
        if (token.Length < 2) return false;
        char first = token[0];
        if (first != '+' && first != '-') return false;
        if (!int.TryParse(token, out stage)) return false;
        return Math.Abs(stage) >= 1 && Math.Abs(stage) <= 6;
    }

    private static BattleState ExtractBattleState(string[] tokens, out string[] remainder)
    {
        var weather = DamageWeather.None;
        bool screens = false;
        var kept = new List<string>();
        int i = 0;
        while (i < tokens.Length)
        {
            var t = tokens[i].ToLowerInvariant();
            if (t == "--weather" && i + 1 < tokens.Length)
            {
                weather = tokens[i + 1].ToLowerInvariant() switch
                {
                    "sun"  => DamageWeather.Sun,
                    "rain" => DamageWeather.Rain,
                    "sand" => DamageWeather.Sand,
                    "snow" or "hail" => DamageWeather.Snow,
                    _ => DamageWeather.None
                };
                i += 2;
                continue;
            }
            if (t == "--screens") { screens = true; i++; continue; }
            kept.Add(tokens[i]);
            i++;
        }
        remainder = [.. kept];
        return new BattleState(weather, screens);
    }

    private async Task<Move?> ResolveMoveAsync(string[] tokens, CancellationToken ct)
    {
        if (tokens.Length == 0) return null;
        var matches = await nameParser.ResolveAsync(tokens, 0, ct);
        var moveMatch = matches.FirstOrDefault(m => m.Type == EntityType.Move);
        return moveMatch?.Entity as Move;
    }

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
