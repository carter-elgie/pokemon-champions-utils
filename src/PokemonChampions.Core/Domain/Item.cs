namespace PokemonChampions.Core.Domain;

/// <summary>Domain model for a held item.</summary>
public class Item
{
    public required string ShowdownId { get; init; }
    public required string Name { get; init; }
    public string? ShortDesc { get; init; }
    public string? Desc { get; init; }
    public bool IsMegaStone { get; init; }
    public string? MegaStoneFor { get; init; }
    public bool IsLegalInCurrentFormat { get; set; }
}
