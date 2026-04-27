using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Calculation;

/// <summary>
/// Pure static stat calculation using the standard Gen 9 formulas at level 50.
/// All methods are deterministic and have no side effects.
/// </summary>
public static class StatCalculator
{
    /// <summary>
    /// Computes the HP stat at a given level using the standard formula:
    /// HP = floor((2*base + iv + floor(ev/4)) * level / 100) + level + 10
    /// </summary>
    public static int CalculateHp(int baseStat, int iv, int ev, int level = AppConstants.LevelCap)
    {
        int inner = (int)Math.Floor((2.0 * baseStat + iv + Math.Floor(ev / 4.0)) * level / 100.0);
        return inner + level + 10;
    }

    /// <summary>
    /// Computes a non-HP stat at a given level:
    /// Stat = floor((floor((2*base + iv + floor(ev/4)) * level / 100) + 5) * natureMult)
    /// </summary>
    public static int CalculateStat(int baseStat, int iv, int ev, double natureMult, int level = AppConstants.LevelCap)
    {
        int inner = (int)Math.Floor((2.0 * baseStat + iv + Math.Floor(ev / 4.0)) * level / 100.0);
        return (int)Math.Floor((inner + 5)  * natureMult);
    }

    /// <summary>
    /// Computes any stat, dispatching to the correct HP or non-HP formula.
    /// </summary>
    public static int Calculate(StatName stat, int baseStat, int iv, int ev, double natureMult = 1.0, int level = AppConstants.LevelCap)
    {
        return stat == StatName.Hp
            ? CalculateHp(baseStat, iv, ev, level)
            : CalculateStat(baseStat, iv, ev, natureMult, level);
    }

    /// <summary>
    /// Converts Champions stat points to the EV value used in the standard Gen 9 formula.
    /// 32 stat points equals 252 EVs; the multiplier of 8 gives 256 EVs, which produces
    /// an identical result to 252 EVs for 31-IV builds at level 50 due to level-50 flooring.
    /// </summary>
    private static int StatPointsToEv(int statPoints) => statPoints * 8;

    /// <summary>
    /// Returns the displayed range for a stat: base value, minimum, and maximum at level 50 with 31 IVs.
    /// Min: 0 stat points, hindering nature (×0.9 for non-HP).
    /// Max: <see cref="AppConstants.MaxStatPointsPerStat"/> stat points, boosting nature (×1.1 for non-HP).
    /// Nature amplifies stat points — they feed into the standard EV slot, not added post-formula.
    /// </summary>
    public static StatRange GetRange(int baseStat, StatName stat, int level = AppConstants.LevelCap)
    {
        int maxEv = StatPointsToEv(AppConstants.MaxStatPointsPerStat);

        if (stat == StatName.Hp)
        {
            int min = CalculateHp(baseStat, AppConstants.MaxIv, 0, level);
            int max = CalculateHp(baseStat, AppConstants.MaxIv, maxEv, level);
            return new StatRange(baseStat, min, max);
        }

        int minVal = CalculateStat(baseStat, AppConstants.MaxIv, 0, 0.9, level);
        int maxVal = CalculateStat(baseStat, AppConstants.MaxIv, maxEv, 1.1, level);
        return new StatRange(baseStat, minVal, maxVal);
    }

    /// <summary>
    /// Computes all six stat ranges for a Pokemon's base stats at level 50.
    /// </summary>
    public static IReadOnlyDictionary<StatName, StatRange> GetAllRanges(BaseStats stats, int level = AppConstants.LevelCap)
    {
        return new Dictionary<StatName, StatRange>
        {
            [StatName.Hp]  = GetRange(stats.Hp,  StatName.Hp,  level),
            [StatName.Atk] = GetRange(stats.Atk, StatName.Atk, level),
            [StatName.Def] = GetRange(stats.Def, StatName.Def, level),
            [StatName.SpA] = GetRange(stats.SpA, StatName.SpA, level),
            [StatName.SpD] = GetRange(stats.SpD, StatName.SpD, level),
            [StatName.Spe] = GetRange(stats.Spe, StatName.Spe, level),
        };
    }

    /// <summary>
    /// Computes actual stats for a specific team member with their stat points, IVs, and nature at level 50.
    /// Stat points feed into the standard EV slot (1 stat point = 8 EVs) and are therefore
    /// amplified by nature like standard EVs.
    /// </summary>
    public static ComputedStats Compute(
        BaseStats baseStats,
        EvSpread statPoints,
        IvSpread ivs,
        Nature nature,
        int level = AppConstants.LevelCap)
    {
        return new ComputedStats(
            Hp:  CalculateHp(baseStats.Hp,  ivs.Hp,  StatPointsToEv(statPoints.Hp),  level),
            Atk: CalculateStat(baseStats.Atk, ivs.Atk, StatPointsToEv(statPoints.Atk), nature.GetMultiplier(StatName.Atk), level),
            Def: CalculateStat(baseStats.Def, ivs.Def, StatPointsToEv(statPoints.Def), nature.GetMultiplier(StatName.Def), level),
            SpA: CalculateStat(baseStats.SpA, ivs.SpA, StatPointsToEv(statPoints.SpA), nature.GetMultiplier(StatName.SpA), level),
            SpD: CalculateStat(baseStats.SpD, ivs.SpD, StatPointsToEv(statPoints.SpD), nature.GetMultiplier(StatName.SpD), level),
            Spe: CalculateStat(baseStats.Spe, ivs.Spe, StatPointsToEv(statPoints.Spe), nature.GetMultiplier(StatName.Spe), level));
    }
}
