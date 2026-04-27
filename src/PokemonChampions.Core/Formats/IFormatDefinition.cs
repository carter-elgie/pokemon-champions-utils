using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Formats;

/// <summary>
/// Defines the rules and legality constraints for a specific competitive format.
/// Implement this interface to add a new format; register it in <see cref="FormatRegistry"/>.
/// </summary>
public interface IFormatDefinition
{
    /// <summary>Showdown-style format identifier, e.g. "gen9championsregma".</summary>
    string ShowdownId { get; }

    /// <summary>Human-readable name, e.g. "[Champions] VGC 2026 Reg M-A".</summary>
    string DisplayName { get; }

    int Generation { get; }
    int LevelCap { get; }

    /// <summary>Size of the team preview pool (typically 6).</summary>
    int TeamPreviewSize { get; }

    /// <summary>Number of Pokemon brought to battle (typically 4 for VGC).</summary>
    int BattleSize { get; }

    bool AllowsMegaEvolution { get; }
    bool AllowsZMoves { get; }
    bool AllowsDynamax { get; }
    bool AllowsTerastal { get; }

    /// <summary>Returns true if the given Pokemon Showdown ID is allowed in this format.</summary>
    bool IsPokemonAllowed(string pokemonShowdownId);

    /// <summary>Returns true if the given move Showdown ID is allowed in this format.</summary>
    bool IsMoveAllowed(string moveShowdownId);

    /// <summary>Returns true if the given item Showdown ID is allowed in this format.</summary>
    bool IsItemAllowed(string itemShowdownId);

    /// <summary>Returns true if the given ability Showdown ID is allowed in this format.</summary>
    bool IsAbilityAllowed(string abilityShowdownId);

    /// <summary>Team-level constraints applied to the full six-member team.</summary>
    IReadOnlyList<ITeamConstraint> TeamConstraints { get; }
}
