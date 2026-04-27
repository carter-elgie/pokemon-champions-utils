namespace PokemonChampions.Core.Domain;

/// <summary>Domain model for a Pokemon ability.</summary>
public class Ability
{
    public required string ShowdownId { get; init; }
    public required string Name { get; init; }
    public string? ShortDesc { get; init; }
    public string? Desc { get; init; }
    public bool IsLegalInCurrentFormat { get; set; }
}
