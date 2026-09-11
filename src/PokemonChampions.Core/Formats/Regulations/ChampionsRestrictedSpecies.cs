namespace PokemonChampions.Core.Formats.Regulations;

/// <summary>
/// Restricted-species ban list shared by Champions regulations that haven't touched
/// this ruling: box legendaries, Treasures of Ruin, Paradox forms, cross-gen
/// legendaries/mythicals, and species with only one legal form.
/// </summary>
internal static class ChampionsRestrictedSpecies
{
    public static readonly HashSet<string> ExplicitlyAllowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "vivillonfancy",
        "taurospaldeacombat",
        "taurospaldeablaze",
        "taurospaldeaaqua",
    };

    public static readonly HashSet<string> BannedShowdownIds = new(StringComparer.OrdinalIgnoreCase)
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
}
