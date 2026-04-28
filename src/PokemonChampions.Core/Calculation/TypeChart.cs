using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Calculation;

/// <summary>
/// Gen 9 type effectiveness chart.
/// Values: 0 = immune, 0.5 = not very effective, 1.0 = neutral, 2.0 = super effective.
/// </summary>
public static class TypeChart
{
    // Row = attacking type, Column = defending type.
    // Order: Normal, Fire, Water, Electric, Grass, Ice, Fighting, Poison,
    //        Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon, Dark, Steel, Fairy
    // PokemonType index: Normal=1 … Fairy=18; array index = (int)type - 1.
    private static readonly double[,] Chart =
    {
        // Normal attacking:
        { 1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1, 0.5,   0,   1,   1, 0.5,   1 },
        // Fire attacking:
        { 1, 0.5, 0.5,   1,   2,   2,   1,   1,   1,   1,   1,   2, 0.5,   1, 0.5,   1,   2,   1 },
        // Water attacking:
        { 1,   2, 0.5,   1, 0.5,   1,   1,   1,   2,   1,   1,   1,   2,   1, 0.5,   1,   1,   1 },
        // Electric attacking:
        { 1,   1,   2, 0.5, 0.5,   1,   1,   1,   0,   2,   1,   1,   1,   1, 0.5,   1,   1,   1 },
        // Grass attacking:
        { 1, 0.5,   2,   1, 0.5,   1,   1, 0.5,   2, 0.5,   1, 0.5,   2,   1, 0.5,   1, 0.5,   1 },
        // Ice attacking:
        { 1, 0.5, 0.5,   1,   2, 0.5,   1,   1,   2,   2,   1,   1,   1,   1,   2,   1, 0.5,   1 },
        // Fighting attacking:
        { 2,   1,   1,   1,   1,   2,   1, 0.5,   1, 0.5, 0.5, 0.5,   2,   0,   1,   2,   2, 0.5 },
        // Poison attacking:
        { 1,   1,   1,   1,   2,   1,   1, 0.5, 0.5,   1,   1,   1, 0.5, 0.5,   1,   1,   0,   2 },
        // Ground attacking:
        { 1,   2,   1,   2, 0.5,   1,   1,   2,   1,   0,   1, 0.5,   2,   1,   1,   1,   2,   1 },
        // Flying attacking:
        { 1,   1,   1, 0.5,   2,   1,   2,   1,   1,   1,   1,   2, 0.5,   1,   1,   1, 0.5,   1 },
        // Psychic attacking:
        { 1,   1,   1,   1,   1,   1,   2,   2,   1,   1, 0.5,   1,   1,   1,   1,   0, 0.5,   1 },
        // Bug attacking:
        { 1, 0.5,   1,   1,   2,   1, 0.5, 0.5,   1, 0.5,   2,   1,   1, 0.5,   1,   2, 0.5, 0.5 },
        // Rock attacking:
        { 1,   2,   1,   1,   1,   2, 0.5,   1, 0.5,   2,   1,   2,   1,   1,   1,   1, 0.5,   1 },
        // Ghost attacking:
        { 0,   1,   1,   1,   1,   1,   1,   1,   1,   1,   2,   1,   1,   2,   1, 0.5,   1,   1 },
        // Dragon attacking:
        { 1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   2,   1, 0.5,   0 },
        // Dark attacking:
        { 1,   1,   1,   1,   1,   1, 0.5,   1,   1,   1,   2, 0.5,   1,   2,   1, 0.5,   1, 0.5 },
        // Steel attacking:
        { 1, 0.5, 0.5, 0.5,   1,   2,   1,   1,   1,   1,   1,   1,   2,   1,   1,   1, 0.5,   2 },
        // Fairy attacking:
        { 1, 0.5,   1,   1,   1,   1,   2, 0.5,   1,   1,   1,   1,   1,   1,   2,   2, 0.5,   1 },
    };

    /// <summary>
    /// Returns the effectiveness multiplier for an attacking type against a defending type.
    /// None and Stellar types always return 1.0.
    /// </summary>
    public static double GetEffectiveness(PokemonType attacker, PokemonType defender)
    {
        int atk = (int)attacker - 1;
        int def = (int)defender - 1;
        if (atk < 0 || atk >= 18 || def < 0 || def >= 18) return 1.0;
        return Chart[atk, def];
    }

    /// <summary>
    /// Returns the combined effectiveness against a dual-typed defender.
    /// Pass PokemonType.None for the second type if the defender is single-typed.
    /// </summary>
    public static double GetCombinedEffectiveness(PokemonType attacker, PokemonType defType1, PokemonType? defType2)
    {
        double eff = GetEffectiveness(attacker, defType1);
        if (defType2.HasValue && defType2.Value != PokemonType.None)
            eff *= GetEffectiveness(attacker, defType2.Value);
        return eff;
    }
}
