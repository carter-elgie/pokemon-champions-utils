namespace PokemonChampions.Core.Domain;

/// <summary>Aggregated usage statistics for a Pokemon in a specific format.</summary>
public class PokemonUsageStats
{
    public required string PokemonShowdownId { get; init; }
    public required string FormatShowdownId { get; init; }
    public double UsagePct { get; init; }
    public string? StatsMonth { get; init; }
    public string? Source { get; init; }

    public IReadOnlyList<UsageEntry> Moves { get; init; } = [];
    public IReadOnlyList<UsageEntry> Items { get; init; } = [];
    public IReadOnlyList<UsageEntry> Abilities { get; init; } = [];
    public IReadOnlyList<SpreadUsageEntry> Spreads { get; init; } = [];
    public IReadOnlyList<UsageEntry> Teammates { get; init; } = [];

    /// <summary>The top-ranked spread, or null if no spread data is available.</summary>
    public SpreadUsageEntry? TopSpread => Spreads.Count > 0 ? Spreads[0] : null;
}

/// <summary>A single usage entry (move, item, or ability) with its usage percentage.</summary>
public record UsageEntry(string Name, string ShowdownId, double UsagePct, int Rank);

/// <summary>An EV spread + nature combination with its usage percentage.</summary>
public record SpreadUsageEntry(
    string Nature,
    EvSpread StatPoints,
    double UsagePct,
    int Rank);
