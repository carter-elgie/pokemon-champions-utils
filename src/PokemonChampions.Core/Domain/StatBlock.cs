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
/// The min and max achievable values for a single stat at level 50 with 31 IVs.
/// Min uses 0 EVs and a hindering nature; max uses 252 EVs and a boosting nature.
/// </summary>
public record StatRange(int Base, int Min, int Max);

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
