using PokemonChampions.Shared.Constants;

namespace PokemonChampions.Core.Domain;

/// <summary>
/// Represents a Pokemon's effort value (stat point) distribution.
/// In Pokemon Champions, stat points cap at <see cref="AppConstants.MaxStatPointsPerStat"/>
/// per stat and <see cref="AppConstants.MaxTotalStatPoints"/> total.
/// </summary>
public record EvSpread(int Hp, int Atk, int Def, int SpA, int SpD, int Spe)
{
    public static readonly EvSpread Zero = new(0, 0, 0, 0, 0, 0);

    public int Total => Hp + Atk + Def + SpA + SpD + Spe;

    /// <summary>Returns true if this spread is within Pokemon Champions' stat point caps.</summary>
    public bool IsLegal() =>
        Total <= AppConstants.MaxTotalStatPoints &&
        Hp <= AppConstants.MaxStatPointsPerStat &&
        Atk <= AppConstants.MaxStatPointsPerStat &&
        Def <= AppConstants.MaxStatPointsPerStat &&
        SpA <= AppConstants.MaxStatPointsPerStat &&
        SpD <= AppConstants.MaxStatPointsPerStat &&
        Spe <= AppConstants.MaxStatPointsPerStat &&
        Hp >= 0 && Atk >= 0 && Def >= 0 && SpA >= 0 && SpD >= 0 && Spe >= 0;

    public override string ToString() =>
        $"{Hp} HP / {Atk} Atk / {Def} Def / {SpA} SpA / {SpD} SpD / {Spe} Spe";
}
