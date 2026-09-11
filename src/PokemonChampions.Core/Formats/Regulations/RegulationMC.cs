using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Formats.Regulations;

/// <summary>
/// Pokemon Champions Regulation M-C (current). Succeeds Reg M-B.
/// Same restricted-species list as prior regulations, plus:
///  - Newly available species: Wigglytuff, Persian/Persian-Alola, Farfetch'd, Mr. Mime,
///    Swalot, Salamence, Gogoat, Golisopod, Rillaboom, Cinderace, Inteleon, Thievul,
///    Toxtricity, Grapploct, Perrserker, Sirfetch'd, Pincurchin, Indeedee (both genders),
///    Pawmot, Arboliva, Squawkabilly, Mabostiff, Baxcalibur.
///  - Newly available items: Leek, Rocky Helmet, Air Balloon, Red Card, Binding Band,
///    Eject Button, Normal Gem, Terrain Extender, Electric Seed, Psychic Seed,
///    Misty Seed, Grassy Seed.
///  - New (custom, non-canonical) Mega Evolutions and their stones: Mega Salamence
///    (Salamencite), Mega Golisopod (Golisopite), Mega Baxcalibur (Baxcalibrite),
///    Mega Garchomp Z (Garchompite Z), Mega Lucario Z (Lucarionite Z), and
///    Mega Absol Z (Absolite Z) — seeded via <see cref="Import.Importers.StaticDataImporter"/>
///    since they don't exist in Pokemon Showdown's data.
/// None of the species/items above were previously blocked by this app's ban-list model
/// (only the restricted-species list below is actually enforced), so no additional
/// gating logic is required for them beyond the mega seed step.
/// </summary>
public sealed class RegulationMC : IFormatDefinition
{
    public string ShowdownId => "gen9championsregmc";
    public string DisplayName => "[Champions] VGC 2026 Reg M-C";

    // MunchStats has not published a format ID for Reg M-C yet.
    public string? MunchStatsFormatId => null;

    public int Generation => 9;
    public int LevelCap => 50;
    public int TeamPreviewSize => 6;
    public int BattleSize => 4;

    public bool AllowsMegaEvolution => true;
    public bool AllowsZMoves => false;
    public bool AllowsDynamax => false;
    public bool AllowsTerastal => false;

    public bool IsPokemonAllowed(string pokemonShowdownId)
    {
        if (ChampionsRestrictedSpecies.ExplicitlyAllowed.Contains(pokemonShowdownId)) return true;
        if (ChampionsRestrictedSpecies.BannedShowdownIds.Contains(pokemonShowdownId)) return false;
        return true;
    }

    public bool IsMoveAllowed(string moveShowdownId) => true; // No specific move bans in Reg M-C

    public bool IsItemAllowed(string itemShowdownId) => true; // No specific item bans in Reg M-C

    public bool IsAbilityAllowed(string abilityShowdownId) => true; // No ability bans

    public IReadOnlyList<ITeamConstraint> TeamConstraints =>
    [
        new NoDuplicateSpeciesConstraint(),
        new NoDuplicateItemsConstraint(),
        new MegaEvolutionLimitConstraint(maxMegas: 1),
    ];
}
