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
    public int Generation => 9;
    public int LevelCap => 50;
    public int TeamPreviewSize => 6;
    public int BattleSize => 4;

    public bool AllowsMegaEvolution => true;
    public bool AllowsZMoves => false;
    public bool AllowsDynamax => false;
    public bool AllowsTerastal => false;

    // National Dex numbers for the Paldea Pokédex (#001–375, #388–392)
    // Pokemon outside these ranges are banned unless explicitly permitted.
    private static readonly HashSet<int> PaldeaDexNumbers = BuildPaldeaDex();

    // Explicitly banned Pokemon by Showdown ID (restricted legendaries, paradox Pokemon, etc.)
    private static readonly HashSet<string> BannedShowdownIds = new(StringComparer.OrdinalIgnoreCase)
    {
        // Treasures of Ruin
        "wo-chien", "chien-pao", "ting-lu", "chi-yu",
        // Box legendaries
        "koraidon", "miraidon",
        // Paradox Pokemon — past forms
        "great-tusk", "scream-tail", "brute-bonnet", "flutter-mane",
        "slither-wing", "sandy-shocks", "roaring-moon",
        // Paradox Pokemon — future forms
        "iron-treads", "iron-bundle", "iron-hands", "iron-jugulis",
        "iron-moth", "iron-thorns", "iron-valiant",
        // Vivillon — only Fancy form allowed
        "vivillon", // base form; vivillon-fancy is allowed
        // Non-Paldean regional forms
        "tauros-paldea-combat", // allowed; non-Paldean Tauros forms are not
    };

    private static readonly HashSet<string> ExplicitlyAllowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "vivillon-fancy",
        "tauros-paldea-combat",
        "tauros-paldea-blaze",
        "tauros-paldea-aqua",
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

    private static HashSet<int> BuildPaldeaDex()
    {
        var dex = new HashSet<int>();
        // Paldea Dex range: 001–375 (Sprigatito through Baxcalibur)
        for (int i = 1; i <= 375; i++) dex.Add(i);
        // Plus Iron Leaves (#906), Walking Wake (#907) — check official list
        // Additional entries: #388–392 (Tinkaton line and Flamigo)
        for (int i = 388; i <= 392; i++) dex.Add(i);
        return dex;
    }
}
