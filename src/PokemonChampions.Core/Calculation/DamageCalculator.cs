using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Calculation;

public enum DamageWeather { None, Sun, Rain, Sand, Snow }

/// <summary>Field conditions that modify damage output.</summary>
public record BattleState(
    DamageWeather Weather = DamageWeather.None,
    bool Screens = false);    // Reflect (Physical) or Light Screen (Special) — caller picks the right one

/// <summary>
/// All information needed to compute a single damage calculation.
/// AttackerStats and DefenderStats should already be computed at the desired investment level.
/// </summary>
public record DamageContext(
    Pokemon AttackerSpecies,
    ComputedStats AttackerStats,
    string? AttackerAbility,
    string? AttackerItem,
    int AttackerStage,
    Pokemon DefenderSpecies,
    ComputedStats DefenderStats,   // .Def / .SpD / .Hp used
    int DefenderStage,
    Move Move,
    BattleState Battle);

/// <summary>Result of a single damage calculation scenario.</summary>
public record DamageResult(
    int MinDamage,
    int MaxDamage,
    int DefenderHp,
    double TypeEffectiveness,
    bool IsStab,
    int EffectiveAttack,
    int EffectiveDefense)
{
    public double MinPct => DefenderHp > 0 ? MinDamage * 100.0 / DefenderHp : 0;
    public double MaxPct => DefenderHp > 0 ? MaxDamage * 100.0 / DefenderHp : 0;
}

public static class DamageCalculator
{
    /// <summary>
    /// Computes Gen 9 VGC damage using the standard formula:
    ///   BaseDamage = floor(floor(22 × BP × A / D) / 50) + 2
    /// then applies spread → weather → random (85-100) → STAB → type → screens.
    /// Returns DamageResult with zero damage for status or zero-power moves.
    /// </summary>
    public static DamageResult Calculate(DamageContext ctx)
    {
        var move = ctx.Move;
        bool isPhysical = move.Category == MoveCategory.Physical;

        if (move.Power is null or 0 || move.Category == MoveCategory.Status)
        {
            return new DamageResult(0, 0, ctx.DefenderStats.Hp,
                TypeChart.GetCombinedEffectiveness(move.Type, ctx.DefenderSpecies.Type1, ctx.DefenderSpecies.Type2),
                IsStab: false, 0, 0);
        }

        string? abilityId = NormalizeId(ctx.AttackerAbility);
        string? itemId    = NormalizeId(ctx.AttackerItem);

        // ── Base power (ability modifiers applied before formula) ─────────────
        int power = move.Power.Value;

        if (abilityId is "technician" && power <= 60)
            power = (int)Math.Floor(power * 1.5);
        if (abilityId is "strongjaw" && move.Flags.Contains("bite") && 
            !move.Name.Equals("bug bite", StringComparison.InvariantCultureIgnoreCase))
            power = (int)Math.Floor(power * 1.5);
        if (abilityId is "ironfist" && move.Flags.Contains("punch") && 
            !move.Name.Equals("sucker punch", StringComparison.InvariantCultureIgnoreCase))
            power = (int)Math.Floor(power * 1.2);
        if (abilityId is "toughclaws" && move.Flags.Contains("contact"))
            power = (int)Math.Floor(power * 1.3);
        if (abilityId is "punkrock" && move.Flags.Contains("sound"))
            power = (int)Math.Floor(power * 1.3);

        // ── Attack stat (stage + item) ────────────────────────────────────────
        int baseAtk = isPhysical ? ctx.AttackerStats.Atk : ctx.AttackerStats.SpA;
        int effectiveAtk = ApplyStage(baseAtk, ctx.AttackerStage);

        if (isPhysical && itemId is "choiceband")   effectiveAtk = (int)Math.Floor(effectiveAtk * 1.5);
        if (!isPhysical && itemId is "choicespecs") effectiveAtk = (int)Math.Floor(effectiveAtk * 1.5);

        // ── Defense stat (stage) ──────────────────────────────────────────────
        int baseDef = isPhysical ? ctx.DefenderStats.Def : ctx.DefenderStats.SpD;
        int effectiveDef = Math.Max(1, ApplyStage(baseDef, ctx.DefenderStage));

        // ── STAB ──────────────────────────────────────────────────────────────
        bool isStab = move.Type == ctx.AttackerSpecies.Type1 ||
                      (ctx.AttackerSpecies.Type2.HasValue &&
                       ctx.AttackerSpecies.Type2 != PokemonType.None &&
                       move.Type == ctx.AttackerSpecies.Type2.Value);
        bool isAdaptability = abilityId is "adaptability";

        // ── Type effectiveness ────────────────────────────────────────────────
        double typeEff = TypeChart.GetCombinedEffectiveness(
            move.Type, ctx.DefenderSpecies.Type1, ctx.DefenderSpecies.Type2);

        // ── Weather multiplier ────────────────────────────────────────────────
        double weatherMult = ctx.Battle.Weather switch
        {
            DamageWeather.Sun  when move.Type == PokemonType.Fire  => 1.5,
            DamageWeather.Sun  when move.Type == PokemonType.Water => 0.5,
            DamageWeather.Rain when move.Type == PokemonType.Water => 1.5,
            DamageWeather.Rain when move.Type == PokemonType.Fire  => 0.5,
            _ => 1.0
        };

        // ── Spread move modifier (doubles) ────────────────────────────────────
        bool isSpread = move.Target is "allAdjacentFoes" or "allAdjacent";

        // ── Base damage ───────────────────────────────────────────────────────
        int inner   = (int)Math.Floor(22.0 * power * effectiveAtk / effectiveDef);
        int baseDmg = (int)Math.Floor(inner / 50.0) + 2;

        if (isSpread)   baseDmg = (int)Math.Floor(baseDmg * 0.75);
        if (weatherMult != 1.0) baseDmg = (int)Math.Floor(baseDmg * weatherMult);

        // ── Random rolls (85–100) ─────────────────────────────────────────────
        int minDmg = (int)Math.Floor(baseDmg * 85.0 / 100.0);
        int maxDmg = baseDmg;

        // ── STAB ──────────────────────────────────────────────────────────────
        if (isStab)
        {
            double stabMult = isAdaptability ? 2.0 : 1.5;
            minDmg = (int)Math.Floor(minDmg * stabMult);
            maxDmg = (int)Math.Floor(maxDmg * stabMult);
        }

        // ── Type effectiveness ────────────────────────────────────────────────
        if (typeEff != 1.0)
        {
            minDmg = (int)Math.Floor(minDmg * typeEff);
            maxDmg = (int)Math.Floor(maxDmg * typeEff);
        }

        // ── Life Orb ──────────────────────────────────────────────────────────
        if (itemId is "lifeorb")
        {
            minDmg = (int)Math.Floor(minDmg * 1.3);
            maxDmg = (int)Math.Floor(maxDmg * 1.3);
        }

        // ── Screens (Reflect/Light Screen) ────────────────────────────────────
        if (ctx.Battle.Screens)
        {
            minDmg = (int)Math.Floor(minDmg * 0.5);
            maxDmg = (int)Math.Floor(maxDmg * 0.5);
        }

        minDmg = Math.Max(typeEff > 0 ? 1 : 0, minDmg);
        maxDmg = Math.Max(typeEff > 0 ? 1 : 0, maxDmg);

        return new DamageResult(
            minDmg, maxDmg, ctx.DefenderStats.Hp,
            typeEff, isStab, effectiveAtk, effectiveDef);
    }

    /// <summary>
    /// Applies a battle stat stage multiplier to a base stat value.
    /// Positive stage n → ×(2+n)/2. Negative stage n → ×2/(2+|n|).
    /// </summary>
    public static int ApplyStage(int stat, int stage)
    {
        if (stage == 0) return stat;
        if (stage > 0)  return (int)Math.Floor(stat * (2.0 + stage) / 2.0);
        return (int)Math.Floor(stat * 2.0 / (2.0 + Math.Abs(stage)));
    }

    private static string? NormalizeId(string? s) =>
        s?.ToLowerInvariant().Replace(" ", "").Replace("-", "");
}
