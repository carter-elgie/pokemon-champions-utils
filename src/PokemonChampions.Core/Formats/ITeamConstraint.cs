using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Formats;

/// <summary>
/// A single team-level rule that can be validated against a full team.
/// Examples: species clause, item clause, mega stone limit.
/// </summary>
public interface ITeamConstraint
{
    string RuleName { get; }

    /// <summary>Returns a list of violation messages, or empty if the team passes this constraint.</summary>
    IReadOnlyList<string> Validate(IReadOnlyList<TeamMember> members);
}
