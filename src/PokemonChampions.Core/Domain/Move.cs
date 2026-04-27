using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Domain;

/// <summary>Domain model for a move.</summary>
public class Move
{
    public required string ShowdownId { get; init; }
    public required string Name { get; init; }
    public required PokemonType Type { get; init; }
    public required MoveCategory Category { get; init; }
    public int? Power { get; init; }
    public int? Accuracy { get; init; }
    public int Pp { get; init; }
    public int Priority { get; init; }
    public string? Target { get; init; }
    public string? ShortDesc { get; init; }
    public string? Desc { get; init; }
    public bool IsLegalInCurrentFormat { get; set; }
}
