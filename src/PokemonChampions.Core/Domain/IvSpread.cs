using PokemonChampions.Shared.Constants;

namespace PokemonChampions.Core.Domain;

/// <summary>Individual value spread for a Pokemon's six stats.</summary>
public record IvSpread(int Hp, int Atk, int Def, int SpA, int SpD, int Spe)
{
    public static readonly IvSpread Perfect = new(
        AppConstants.MaxIv, AppConstants.MaxIv, AppConstants.MaxIv,
        AppConstants.MaxIv, AppConstants.MaxIv, AppConstants.MaxIv);

    /// <summary>
    /// Perfect IVs except 0 Attack — used by special attackers to minimize
    /// Foul Play damage and confusion self-damage.
    /// </summary>
    public static readonly IvSpread PerfectNoAtk = Perfect with { Atk = 0 };
}
