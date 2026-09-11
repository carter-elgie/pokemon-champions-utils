using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Formats.Regulations;

/// <summary>
/// Pokemon Champions Regulation M-A (April 8 – June 17, 2026).
/// Paldea Pokédex only, Mega Evolution allowed, Doubles 4v4 from 6 at Level 50.
/// </summary>
public sealed class RegulationMA : IFormatDefinition
{
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

    public bool IsPokemonAllowed(string pokemonShowdownId)
    {
        if (ChampionsRestrictedSpecies.ExplicitlyAllowed.Contains(pokemonShowdownId)) return true;
        if (ChampionsRestrictedSpecies.BannedShowdownIds.Contains(pokemonShowdownId)) return false;
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
