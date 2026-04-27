using PokemonChampions.Core.Domain;

namespace PokemonChampions.Core.Formats;

/// <summary>
/// Enforces that no two team members share the same species (ignoring form differences
/// for base species comparisons).
/// </summary>
public sealed class NoDuplicateSpeciesConstraint : ITeamConstraint
{
    public string RuleName => "Species Clause";

    public IReadOnlyList<string> Validate(IReadOnlyList<TeamMember> members)
    {
        var violations = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in members.Where(m => m.PokemonShowdownId != null))
        {
            var speciesId = GetBaseSpecies(member.PokemonShowdownId!);
            if (!seen.Add(speciesId))
                violations.Add($"Species Clause: {member.PokemonShowdownId} appears more than once.");
        }

        return violations;
    }

    // Strips mega/gmax suffixes to get the base species for clause checking.
    private static string GetBaseSpecies(string showdownId)
    {
        var idx = showdownId.IndexOf('-');
        return idx < 0 ? showdownId : showdownId[..idx];
    }
}

/// <summary>Enforces that no two team members hold the same item.</summary>
public sealed class NoDuplicateItemsConstraint : ITeamConstraint
{
    public string RuleName => "Item Clause";

    public IReadOnlyList<string> Validate(IReadOnlyList<TeamMember> members)
    {
        var violations = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in members.Where(m => !string.IsNullOrEmpty(m.Item)))
        {
            if (!seen.Add(member.Item!))
                violations.Add($"Item Clause: {member.Item} is held by more than one Pokemon.");
        }

        return violations;
    }
}

/// <summary>Enforces that at most one team member holds a Mega Stone.</summary>
public sealed class MegaEvolutionLimitConstraint : ITeamConstraint
{
    private readonly int _maxMegas;

    public MegaEvolutionLimitConstraint(int maxMegas = 1) => _maxMegas = maxMegas;

    public string RuleName => "Mega Evolution Limit";

    public IReadOnlyList<string> Validate(IReadOnlyList<TeamMember> members)
    {
        // The actual mega stone item check requires item metadata; this is a stub
        // that can be enhanced once item data is loaded.
        return [];
    }
}
