using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Formats.Regulations;

/// <summary>
/// Pokemon Champions Regulation M-A (April 8 – June 17, 2026).
/// Paldea Pokédex only, Mega Evolution allowed, Doubles 4v4 from 6 at Level 50.
/// </summary>
public sealed class RegulationMA : IFormatDefinition
{
    // Seeded from the database on first access; populated by the update command.
    private IReadOnlySet<string>? _paldeaDex;

    public RegulationMA(IReadOnlySet<string>? paldeaDexIds = null)
    {
        _paldeaDex = paldeaDexIds;
    }

    public string ShowdownId => "gen9championsregma";
    public string DisplayName => "[Champions] VGC 2026 Reg M-A";
    public string? MunchStatsFormatId => "gen9championsvgc2026regma";
    public int Generation => 9;
    public int LevelCap => 50;
    public int TeamPreviewSize => 6;
    public int BattleSize => 4;

    public bool AllowsMegaEvolution => true;
    public bool AllowsZMoves => false;
    public bool AllowsDynamax => false;
    public bool AllowsTerastal => false;

    // Showdown IDs are lowercase with no hyphens, matching the DB ShowdownId column.
    // e.g. Wo-Chien → "wochien", Great Tusk → "greattusk"

    private static readonly HashSet<string> ExplicitlyAllowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "vivillonfancy",
        "taurospaldeacombat",
        "taurospaldeablaze",
        "taurospaldeaaqua",
    };

    private static readonly HashSet<string> BannedShowdownIds = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Gen 9 restricted ────────────────────────────────────────────────
        // Treasures of Ruin
        "wochien", "chienpao", "tinglu", "chiyu",
        // Box legendaries
        "koraidon", "miraidon",
        // Paradox Pokemon — past forms
        "greattusk", "screamtail", "brutebonnet", "fluttermane",
        "slitherwing", "sandyshocks", "roaringmoon",
        // Paradox Pokemon — future forms
        "irontreads", "ironbundle", "ironhands", "ironjugulis",
        "ironmoth", "ironthorns", "ironvaliant",
        // DLC legendaries
        "ogerpon", "ogerponwellspring", "ogerponhearthflame", "ogerponcornerstone",
        "terapagos", "terapagosstellar",
        "pecharunt",

        // ── Cross-gen legendaries available in SV ───────────────────────────
        // Gen 1
        "articuno", "zapdos", "moltres", "mewtwo", "mew",
        // Gen 2
        "raikou", "entei", "suicune", "lugia", "hooh", "celebi",
        // Gen 3
        "regirock", "regice", "registeel",
        "latias", "latios", "kyogre", "groudon", "rayquaza",
        "jirachi",
        "deoxys", "deoxysattack", "deoxysdefense", "deoxysspeed",
        // Gen 4
        "uxie", "mesprit", "azelf",
        "dialga", "dialgaorigin", "palkia", "palkiaorigin",
        "heatran", "regigigas", "giratina", "giratinaorigin", "cresselia",
        "phione", "manaphy", "darkrai", "shaymin", "shayminsky", "arceus",
        // Gen 5
        "cobalion", "terrakion", "virizion",
        "tornadus", "tornadustherian", "thundurus", "thundurustherian",
        "landorus", "landorustherian",
        "reshiram", "zekrom", "kyurem", "kyuremblack", "kyuremwhite",
        "keldeo", "keldeoresolute",
        "meloetta", "meloettapirouette",
        "genesect", "genesectburn", "genesectchill", "genesectdouse", "genesectshock",
        // Gen 6
        "xerneas", "yveltal",
        "zygarde", "zygarde10", "zygardecomplete",
        "diancie", "hoopa", "hoopaunbound", "volcanion",
        // Gen 7
        "solgaleo", "lunala",
        "necrozma", "necrozmaduskmane", "necrozmadawnwings", "necrozmaultra",
        "magearna", "marshadow", "zeraora", "meltan", "melmetal",
        // Gen 8
        "zacian", "zaciancrowned", "zamazenta", "zamazentacrowned", "eternatus",
        "kubfu", "urshifu", "urshifurapidstrike",
        "zarude", "zarudedada",
        "regieleki", "regidrago",
        "glastrier", "spectrier", "calyrex", "calyrexice", "calyrexshadow",
        "enamorus", "enamorustherian",

        // ── Vivillon — only Fancy form allowed ──────────────────────────────
        "vivillon",
        // Non-Paldean Tauros
        "tauros",
    };

    public void SetPaldeaDex(IReadOnlySet<string> paldeaDexIds) => _paldeaDex = paldeaDexIds;

    public bool IsPokemonAllowed(string pokemonShowdownId)
    {
        if (ExplicitlyAllowed.Contains(pokemonShowdownId)) return true;
        if (BannedShowdownIds.Contains(pokemonShowdownId)) return false;

        // If we have the DB-seeded dex list, use it for accurate checks.
        if (_paldeaDex != null)
            return _paldeaDex.Contains(pokemonShowdownId.ToLowerInvariant());

        // Fallback: allow if not explicitly banned. The update command seeds the dex list.
        return true;
    }

    public bool IsMoveAllowed(string moveShowdownId) => true; // No specific move bans in Reg M-A

    public bool IsItemAllowed(string itemShowdownId) => true; // No specific item bans in Reg M-A

    public bool IsAbilityAllowed(string abilityShowdownId) => true; // No ability bans

    public IReadOnlyList<ITeamConstraint> TeamConstraints =>
    [
        new NoDuplicateSpeciesConstraint(),
        new NoDuplicateItemsConstraint(),
        new MegaEvolutionLimitConstraint(maxMegas: 1),
    ];
}
