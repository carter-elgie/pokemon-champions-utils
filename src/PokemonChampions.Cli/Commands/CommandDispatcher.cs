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
    IItemService itemService,
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
          [bold]                 [[modifiers...]][/]      Speed: scarf, tailwind, para, chlorophyll, swift swim, sand rush,
          [bold]                                [/]        slush rush, unburden, surge surfer, quick feet, slow start, iron ball
          [bold]                                [/]      Atk: band, huge power, hustle, gorilla tactics, guts, defeatist,
          [bold]                                [/]        flower gift, light ball, thick club
          [bold]                                [/]      SpA: specs, solar power, plus, minus, hadron engine, defeatist, light ball
          [bold]                                [/]      Def: fur coat, marvel scale, eviolite
          [bold]                                [/]      SpD: vest, ice scales, eviolite
          [bold]                                [/]      All: +N/-N (stat stage, ±1–6)
          [bold]<atk> [[+N]] <move> > <def> [[-N]][/]    Outgoing damage (left=your pokemon, right=opponent)
          [bold]<def> < <atk> [[+N]] <move>[/]           Incoming damage (left=your pokemon, right=opponent)
          [bold]             [[--weather sun|rain|sand|snow]] [[--terrain electric|grassy|psychic|misty]][/]
          [bold]             [[--screens]] [[--aurora-veil]] [[--gravity]][/]
          [bold]             [[--crit]] [[--burned]] [[--paralyzed]] [[--poisoned]][/]
          [bold]             [[--helping-hand]] [[--parental-bond]] [[--glaive-rush]][/]
          [bold]             [[--friend-guard]] [[--ally-battery]] [[--ally-power-spot]] [[--ally-steely-spirit]][/]
          [bold]             [[--analytic]] [[--charge]] [[--sheer-force]] [[--metronome N]][/]
          [bold]             [[--atk-form <form>]] [[--def-form <form>]][/]
          [bold]             [[--ability <ability>]][/]
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
        // Fetch the queried Pokemon's own team build (if on team) for the "Your build" header line.
        // The queried Pokemon is excluded from the tier entries to avoid duplication.
        int? teamBuildStat = null;
        string? teamBuildLabel = null;
        var ownMember = await GetActiveTeamMemberAsync(pokemon.ShowdownId, ct);
        if (ownMember?.Nature is not null)
        {
            var nature = Nature.TryGet(ownMember.Nature) ?? new Nature("?", null, null);
            int raw = StatCalculator.Compute(pokemon.BaseStats, ownMember.StatPoints, ownMember.Ivs, nature).Get(stat);
            teamBuildStat = modifiers.Apply(raw, stat, pokemon);
            teamBuildLabel = BuildTeamBuildStatLabel(ownMember, nature, stat);
        }

        var entries = await GetTeamEntriesAsync(stat, ct, excludeShowdownId: pokemon.ShowdownId);
        PokemonRenderer.RenderStatTier(pokemon, stat, entries, modifiers, teamBuildStat, teamBuildLabel);
    }

    private async Task RenderStatBuildAsync(
        Pokemon pokemon, StatName stat, BuildSpec build, StatModifierSet modifiers, CancellationToken ct)
    {
        int baseStat = pokemon.BaseStats.Get(stat);
        int evOrSp = build.EvOrSp ?? 0;
        int rawEv = evOrSp <= AppConstants.MaxStatPointsPerStat ? evOrSp * 8 : evOrSp;
        double natureMult = stat != StatName.Hp ? (build.Nature?.GetMultiplier(stat) ?? 1.0) : 1.0;
        int unmodified = StatCalculator.Calculate(stat, baseStat, AppConstants.MaxIv, rawEv, natureMult);

        // Include all team entries (including the queried Pokemon's actual build if different)
        var entries = await GetTeamEntriesAsync(stat, ct);
        PokemonRenderer.RenderStatBuild(
            pokemon, stat, modifiers.Apply(unmodified, stat, pokemon), unmodified,
            BuildLabelFor(build, stat), entries, modifiers);
    }

    private async Task<List<StatTierEntry>> GetTeamEntriesAsync(
        StatName stat, CancellationToken ct, string? excludeShowdownId = null)
    {
        var entries = new List<StatTierEntry>();
        var team = await teamService.GetActiveAsync(ct);
        if (team is null) return entries;

        foreach (var member in team.Members)
        {
            if (member.PokemonShowdownId is null) continue;
            if (excludeShowdownId is not null &&
                string.Equals(member.PokemonShowdownId, excludeShowdownId, StringComparison.OrdinalIgnoreCase))
                continue;

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

    private static string BuildTeamBuildStatLabel(TeamMember member, Nature nature, StatName stat)
    {
        var parts = new List<string>();
        if (!nature.IsNeutral && stat != StatName.Hp) parts.Add(nature.Name);
        int sp = member.StatPoints.Get(stat);
        if (sp > 0) parts.Add($"{sp} SP");
        return parts.Count > 0 ? string.Join(", ", parts) : "team build";
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
    /// Handles "attacker [+N] move > defender [-N] [--flags]"  (isOutgoing=true)
    /// and    "defender < attacker [+N] move [--flags]"         (isOutgoing=false).
    ///
    /// Direction determines which side is "ours":
    ///   >  Left side is our Pokemon (uses team build if on team). Right side is always the opponent.
    ///   &lt;  Left side is our Pokemon (uses team build if on team). Right side is always the opponent.
    ///
    /// This means the stat stages (+N/-N) in the expression refer to the relevant offensive stat
    /// for the attacker and the relevant defensive stat for the defender:
    ///   - Physical moves: Attack stage / Defense stage
    ///   - Special moves:  Sp. Atk stage / Sp. Def stage
    ///   - Body Press:     Defense stage (used as offense) / Defense stage
    /// </summary>
    private async Task HandleDamageCalcAsync(string[] allTokens, int opIdx, bool isOutgoing, CancellationToken ct)
    {
        var flags = ExtractCalcFlags(allTokens, out var cleanTokens);
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
        bool isBodyPress = string.Equals(
            move.ShowdownId.Replace("-", ""), "bodypress", StringComparison.OrdinalIgnoreCase);

        // ── Look up active team ────────────────────────────────────────────────
        var activeTeam = await teamService.GetActiveAsync(ct);

        // Left side is always "our" Pokemon. Only look up a team member for the left side.
        //   >  form: left = attacker (ours), right = defender (opponent)
        //   <  form: left = defender (ours), right = attacker (opponent)
        var atkMember = isOutgoing
            ? activeTeam?.Members.FirstOrDefault(m =>
                string.Equals(m.PokemonShowdownId, attacker.ShowdownId, StringComparison.OrdinalIgnoreCase))
            : null;
        var defMember = !isOutgoing
            ? activeTeam?.Members.FirstOrDefault(m =>
                string.Equals(m.PokemonShowdownId, defender.ShowdownId, StringComparison.OrdinalIgnoreCase))
            : null;

        // ── Resolve attacker stats / form / ability / item ────────────────────
        ComputedStats atkStats;
        string attackerLabel;
        string? atkAbility, atkItem;

        if (atkMember is not null && atkMember.Nature is not null)
        {
            var nature = Nature.TryGet(atkMember.Nature) ?? new Nature("?", null, null);

            // Resolve form: explicit --atk-form override, then auto-detect mega from item
            var atkForm = await ResolveFormAsync(attacker, flags.AtkForm, atkMember.Item, ct);
            if (atkForm is not null) attacker = atkForm;

            atkStats   = StatCalculator.Compute(attacker.BaseStats, atkMember.StatPoints, atkMember.Ivs, nature);
            atkAbility = atkForm?.IsMega == true ? atkForm.Ability0 : atkMember.Ability;
            atkItem    = atkMember.Item;
            attackerLabel = BuildAttackerLabel(atkMember, nature, isPhysical, isBodyPress, atkAbility);
        }
        else
        {
            // Apply explicit form override even for non-team attackers
            if (flags.AtkForm is not null)
            {
                var overrideAtk = await pokemonService.FindAsync(flags.AtkForm, ct);
                if (overrideAtk is not null) attacker = overrideAtk;
            }

            int maxEv  = AppConstants.MaxStatPointsPerStat * 8;
            int maxAtk = StatCalculator.CalculateStat(attacker.BaseStats.Atk, AppConstants.MaxIv, maxEv, 1.1);
            int maxSpA = StatCalculator.CalculateStat(attacker.BaseStats.SpA, AppConstants.MaxIv, maxEv, 1.1);
            int maxDef = StatCalculator.CalculateStat(attacker.BaseStats.Def, AppConstants.MaxIv, maxEv, 1.1);
            atkStats      = new ComputedStats(0, maxAtk, maxDef, maxSpA, 0, 0);
            atkItem       = null;
            // For non-team (left side in > form): use Ability0 as default
            // For opponent attacker (right side in < form): use --opp-ability or default
            atkAbility = !isOutgoing
                ? (flags.OppAbility ?? await GetDefaultAbilityAsync(attacker, ct))
                : attacker.Ability0;
            attackerLabel = isBodyPress
                ? $"max Def ({maxDef})"
                : (isPhysical ? $"max Atk ({maxAtk})" : $"max SpA ({maxSpA})");
        }

        // ── Resolve defender form / ability ────────────────────────────────────
        string? defAbility;

        if (defMember is not null)
        {
            // Our defender (< form) — resolve form and use team ability
            var defForm = await ResolveFormAsync(defender, flags.DefForm, defMember.Item, ct);
            if (defForm is not null) defender = defForm;
            defAbility = defForm?.IsMega == true ? defForm.Ability0 : defMember.Ability;
        }
        else
        {
            // Apply explicit form override for the defender
            if (flags.DefForm is not null)
            {
                var overrideDef = await pokemonService.FindAsync(flags.DefForm, ct);
                if (overrideDef is not null) defender = overrideDef;
            }

            // Opponent defender (right side in > form): use --opp-ability or usage-stats default
            defAbility = isOutgoing
                ? (flags.OppAbility ?? await GetDefaultAbilityAsync(defender, ct))
                : defender.Ability0;
        }

        // ── Build DamageContext helper ─────────────────────────────────────────
        DamageContext MakeCtx(ComputedStats defStats) => new(
            attacker, atkStats, atkAbility, atkItem, atkStage,
            defender, defStats, defStage, move, flags.Battle,
            DefenderAbility:         defAbility,
            DefenderAtFullHp:        true,
            IsBurned:                flags.IsBurned,
            IsParalyzed:             flags.IsParalyzed,
            IsPoisoned:              flags.IsPoisoned,
            IsCritical:              flags.IsCritical,
            IsHelpingHand:           flags.IsHelpingHand,
            IsParentalBondSecondHit: flags.IsParentalBondSecondHit,
            AllyHasFriendGuard:      flags.AllyHasFriendGuard,
            AllyHasBattery:          flags.AllyHasBattery,
            AllyHasPowerSpot:        flags.AllyHasPowerSpot,
            AllyHasSteellySpirit:    flags.AllyHasSteellySpirit,
            GlaiveRush:              flags.GlaiveRush,
            TargetMovedFirst:        flags.TargetMovedFirst,
            IsCharged:               flags.IsCharged,
            HasSheerForceBoost:      flags.HasSheerForceBoost,
            MetronomeCount:          flags.MetronomeCount);

        // ── Resolve defender scenarios ─────────────────────────────────────────
        var scenarios = new List<(string Label, DamageResult Result)>();

        if (defMember is not null && defMember.Nature is not null)
        {
            var defNature = Nature.TryGet(defMember.Nature) ?? new Nature("?", null, null);
            var defStats  = StatCalculator.Compute(defender.BaseStats, defMember.StatPoints, defMember.Ivs, defNature);
            scenarios.Add(("Your " + defMember.DisplayName(defender.Name), DamageCalculator.Calculate(MakeCtx(defStats))));
        }
        else
        {
            int maxEv        = AppConstants.MaxStatPointsPerStat * 8;
            StatName defStatName = isPhysical ? StatName.Def : StatName.SpD;
            int baseDefStat  = isPhysical ? defender.BaseStats.Def : defender.BaseStats.SpD;

            int minDefStat = StatCalculator.CalculateStat(baseDefStat, AppConstants.MaxIv, 0, 0.9);
            int minHp      = StatCalculator.CalculateHp(defender.BaseStats.Hp, AppConstants.MaxIv, 0);
            var minStats   = isPhysical
                ? new ComputedStats(minHp, 0, minDefStat, 0, 0, 0)
                : new ComputedStats(minHp, 0, 0, 0, minDefStat, 0);
            scenarios.Add(($"0 {defStatName} ({minDefStat} / {minHp} HP)", DamageCalculator.Calculate(MakeCtx(minStats))));

            int maxDefStat = StatCalculator.CalculateStat(baseDefStat, AppConstants.MaxIv, maxEv, 1.1);
            int maxHp      = StatCalculator.CalculateHp(defender.BaseStats.Hp, AppConstants.MaxIv, maxEv);
            var maxStats   = isPhysical
                ? new ComputedStats(maxHp, 0, maxDefStat, 0, 0, 0)
                : new ComputedStats(maxHp, 0, 0, 0, maxDefStat, 0);
            scenarios.Add(($"Max {defStatName} ({maxDefStat} / {maxHp} HP)", DamageCalculator.Calculate(MakeCtx(maxStats))));
        }

        DamageRenderer.Render(move, attacker, attackerLabel, atkAbility, defender, defAbility,
                              scenarios, flags.Battle, !isOutgoing);
    }

    /// <summary>
    /// Resolves the Pokemon to use for a calc participant, applying form changes.
    /// Checks <paramref name="formOverride"/> first (explicit --atk-form / --def-form flag),
    /// then auto-detects Mega Evolution from the held item.
    /// Returns null if no form change applies.
    /// </summary>
    private async Task<Pokemon?> ResolveFormAsync(
        Pokemon baseSpecies, string? formOverride, string? heldItem, CancellationToken ct)
    {
        // Explicit form name takes priority over auto-detection
        if (formOverride is not null)
            return await pokemonService.FindAsync(formOverride, ct);

        if (heldItem is null) return null;

        // Check if the held item is a Mega Stone for this Pokemon
        var itemObj = await itemService.FindAsync(heldItem, ct);
        if (itemObj is null || !itemObj.IsMegaStone) return null;
        if (!string.Equals(itemObj.MegaStoneFor, baseSpecies.ShowdownId, StringComparison.OrdinalIgnoreCase))
            return null;

        // Derive mega form ID from base species + item suffix.
        // For two-mega Pokemon (Charizard, Mewtwo): item ends with "x"/"y" → form ends with "-mega-x"/"y".
        // For single-mega Pokemon (Blastoise, Venusaur, …): try "{base}-mega".
        var itemNorm = heldItem.ToLowerInvariant().Replace("-", "").Replace(" ", "");
        var candidates = new List<string>();

        if (itemNorm.Length > 0)
        {
            char last = itemNorm[^1];
            if (last == 'x' || last == 'y')
                candidates.Add($"{baseSpecies.ShowdownId}-mega-{last}");
        }
        candidates.Add($"{baseSpecies.ShowdownId}-mega");

        foreach (var candidate in candidates)
        {
            var form = await pokemonService.FindAsync(candidate, ct);
            if (form is not null) return form;
        }
        return null;
    }

    /// <summary>
    /// Returns the default ability to assume for an opponent Pokemon.
    /// Uses the top-usage ability if online data is available; otherwise falls back to Ability0.
    /// </summary>
    private async Task<string?> GetDefaultAbilityAsync(Pokemon species, CancellationToken ct)
    {
        var formatId = await settingsService.GetAsync(AppConstants.SettingKeys.CurrentFormat, ct)
                       ?? AppConstants.DefaultFormat;
        var stats = await usageStatsService.GetAsync(species.ShowdownId, formatId, ct);
        if (stats?.Abilities.Count > 0)
            return stats.Abilities[0].ShowdownId;
        return species.Ability0;
    }

    private static string BuildAttackerLabel(
        TeamMember member, Nature nature, bool isPhysical, bool isBodyPress, string? ability)
    {
        var parts = new List<string>();
        if (!nature.IsNeutral) parts.Add(nature.Name);
        var sp = isBodyPress ? member.StatPoints.Def
                             : (isPhysical ? member.StatPoints.Atk : member.StatPoints.SpA);
        string statLabel = isBodyPress ? "Def" : (isPhysical ? "Atk" : "SpA");
        if (sp > 0) parts.Add($"{sp} SP {statLabel}");
        if (ability is not null) parts.Add(ability);
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

    private record CalcFlags(
        BattleState Battle,
        bool IsCritical,
        bool IsBurned,
        bool IsParalyzed,
        bool IsPoisoned,
        bool IsHelpingHand,
        bool IsParentalBondSecondHit,
        bool GlaiveRush,
        bool AllyHasFriendGuard,
        bool AllyHasBattery,
        bool AllyHasPowerSpot,
        bool AllyHasSteellySpirit,
        bool TargetMovedFirst,
        bool IsCharged,
        bool HasSheerForceBoost,
        int MetronomeCount,
        string? AtkForm,
        string? DefForm,
        string? OppAbility);

    private static CalcFlags ExtractCalcFlags(string[] tokens, out string[] remainder)
    {
        var weather          = DamageWeather.None;
        var terrain          = DamageTerrain.None;
        bool screens         = false;
        bool auroraVeil      = false;
        bool gravity         = false;
        bool isCrit          = false;
        bool isBurned        = false;
        bool isParalyzed     = false;
        bool isPoisoned      = false;
        bool helpingHand     = false;
        bool parentalBond    = false;
        bool glaiveRush      = false;
        bool friendGuard     = false;
        bool allyBattery     = false;
        bool allyPowerSpot   = false;
        bool allySteelSpirit = false;
        bool analytic        = false;
        bool charge          = false;
        bool sheerForce      = false;
        int  metronomeCount  = 0;
        string? atkForm      = null;
        string? defForm      = null;
        string? oppAbility   = null;
        var kept = new List<string>();
        int i = 0;
        while (i < tokens.Length)
        {
            var t = tokens[i].ToLowerInvariant();
            if (t == "--weather" && i + 1 < tokens.Length)
            {
                weather = tokens[i + 1].ToLowerInvariant() switch
                {
                    "sun"            => DamageWeather.Sun,
                    "rain"           => DamageWeather.Rain,
                    "sand"           => DamageWeather.Sand,
                    "snow" or "hail" => DamageWeather.Snow,
                    _                => DamageWeather.None
                };
                i += 2; continue;
            }
            if (t == "--terrain" && i + 1 < tokens.Length)
            {
                terrain = tokens[i + 1].ToLowerInvariant() switch
                {
                    "electric" => DamageTerrain.Electric,
                    "grassy"   => DamageTerrain.Grassy,
                    "psychic"  => DamageTerrain.Psychic,
                    "misty"    => DamageTerrain.Misty,
                    _          => DamageTerrain.None
                };
                i += 2; continue;
            }
            if (t == "--metronome" && i + 1 < tokens.Length &&
                int.TryParse(tokens[i + 1], out int mc) && mc >= 1)
            {
                metronomeCount = mc; i += 2; continue;
            }
            if (t == "--atk-form" && i + 1 < tokens.Length)
            {
                atkForm = tokens[i + 1]; i += 2; continue;
            }
            if (t == "--def-form" && i + 1 < tokens.Length)
            {
                defForm = tokens[i + 1]; i += 2; continue;
            }
            if (t == "--ability" && i + 1 < tokens.Length)
            {
                oppAbility = tokens[i + 1]; i += 2; continue;
            }
            switch (t)
            {
                case "--screens":            screens         = true; break;
                case "--aurora-veil":        auroraVeil      = true; break;
                case "--gravity":            gravity         = true; break;
                case "--crit":               isCrit          = true; break;
                case "--burned":             isBurned        = true; break;
                case "--paralyzed":          isParalyzed     = true; break;
                case "--poisoned":           isPoisoned      = true; break;
                case "--helping-hand":       helpingHand     = true; break;
                case "--parental-bond":      parentalBond    = true; break;
                case "--glaive-rush":        glaiveRush      = true; break;
                case "--friend-guard":       friendGuard     = true; break;
                case "--ally-battery":       allyBattery     = true; break;
                case "--ally-power-spot":    allyPowerSpot   = true; break;
                case "--ally-steely-spirit": allySteelSpirit = true; break;
                case "--analytic":           analytic        = true; break;
                case "--charge":             charge          = true; break;
                case "--sheer-force":        sheerForce      = true; break;
                default: kept.Add(tokens[i]); break;
            }
            i++;
        }
        remainder = [.. kept];
        return new CalcFlags(
            new BattleState(weather, terrain, screens, auroraVeil, gravity),
            isCrit, isBurned, isParalyzed, isPoisoned,
            helpingHand, parentalBond, glaiveRush, friendGuard,
            allyBattery, allyPowerSpot, allySteelSpirit,
            analytic, charge, sheerForce, metronomeCount,
            atkForm, defForm, oppAbility);
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
