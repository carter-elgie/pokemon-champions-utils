namespace PokemonChampions.Core.Domain;

/// <summary>Domain model for a user-created team.</summary>
public class Team
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? FormatShowdownId { get; init; }
    public required string Pokepaste { get; init; }
    public List<TeamMember> Members { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Domain model for a single Pokemon on a team.</summary>
public class TeamMember
{
    public int Id { get; init; }
    public int TeamId { get; init; }
    public string? PokemonShowdownId { get; init; }
    public string? Nickname { get; init; }
    public string? Item { get; init; }
    public string? Ability { get; init; }
    public string? Nature { get; init; }
    public required EvSpread StatPoints { get; init; }
    public required IvSpread Ivs { get; init; }
    public string? Move1 { get; init; }
    public string? Move2 { get; init; }
    public string? Move3 { get; init; }
    public string? Move4 { get; init; }
    public int SlotIndex { get; init; }
    public bool? IsLegal { get; set; }
    public string? LegalityNotes { get; set; }

    public IEnumerable<string> GetMoves()
    {
        if (Move1 != null) yield return Move1;
        if (Move2 != null) yield return Move2;
        if (Move3 != null) yield return Move3;
        if (Move4 != null) yield return Move4;
    }

    /// <summary>
    /// The display name for this team member — nickname if set, otherwise the Pokemon's name.
    /// </summary>
    public string DisplayName(string pokemonName) =>
        !string.IsNullOrWhiteSpace(Nickname) ? Nickname : pokemonName;
}
