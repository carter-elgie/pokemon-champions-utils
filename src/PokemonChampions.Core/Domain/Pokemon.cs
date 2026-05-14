using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Domain;

/// <summary>Domain model for a Pokemon species (not a team member instance).</summary>
public class Pokemon
{
    public required string ShowdownId { get; init; }
    public required string Name { get; init; }
    public required PokemonType Type1 { get; init; }
    public PokemonType? Type2 { get; init; }
    public required BaseStats BaseStats { get; init; }
    public string? Ability0 { get; init; }
    public string? Ability1 { get; init; }
    public string? AbilityH { get; init; }
    public bool IsMega { get; init; }
    public string? BaseFormShowdownId { get; init; }
    public bool CanEvolve { get; init; }
    public bool IsLegalInCurrentFormat { get; set; }

    public IEnumerable<string> GetAbilities()
    {
        if (Ability0 != null) yield return Ability0;
        if (Ability1 != null && Ability1 != Ability0) yield return Ability1;
        if (AbilityH != null) yield return AbilityH;
    }
}
