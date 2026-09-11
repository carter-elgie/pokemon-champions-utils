using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Domain;

/// <summary>Base stats for a Pokemon.</summary>
public record BaseStats(int Hp, int Atk, int Def, int SpA, int SpD, int Spe)
{
    public int Get(StatName stat) => stat switch
    {
        StatName.Hp  => Hp,
        StatName.Atk => Atk,
        StatName.Def => Def,
        StatName.SpA => SpA,
        StatName.SpD => SpD,
        StatName.Spe => Spe,
        _ => throw new ArgumentOutOfRangeException(nameof(stat))
    };

    public int Total => Hp + Atk + Def + SpA + SpD + Spe;
}

/// <summary>
/// The achievable values for a single stat at level 50 with 31 IVs.
/// Min: 0 stat points, hindering nature. SoftMin: 0 stat points, neutral nature.
/// SoftMax: max stat points, neutral nature. Max: max stat points, boosting nature.
/// For HP (which nature never affects), Min == SoftMin and SoftMax == Max.
/// </summary>
public record StatRange(int Base, int Min, int SoftMin, int SoftMax, int Max);

/// <summary>Computed stats for a specific Pokemon instance at level 50.</summary>
public record ComputedStats(int Hp, int Atk, int Def, int SpA, int SpD, int Spe)
{
    public int Get(StatName stat) => stat switch
    {
        StatName.Hp  => Hp,
        StatName.Atk => Atk,
        StatName.Def => Def,
        StatName.SpA => SpA,
        StatName.SpD => SpD,
        StatName.Spe => Spe,
        _ => throw new ArgumentOutOfRangeException(nameof(stat))
    };
}

/// <summary>
/// One row in a stat tier comparison list, representing a team member alongside the queried Pokemon.
/// Actual is the computed stat from the member's actual build; null if the member has no build data.
/// </summary>
public record StatTierEntry(string Name, int Min, int Max, int? Actual);
