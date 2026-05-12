using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Calculation;

public enum DamageWeather { None, Sun, Rain, Sand, Snow }
public enum DamageTerrain { None, Electric, Grassy, Psychic, Misty }

/// <summary>Field conditions that modify damage output.</summary>
public record BattleState(
    DamageWeather Weather = DamageWeather.None,
    DamageTerrain Terrain = DamageTerrain.None,
    bool Screens    = false,    // Reflect (Physical) or Light Screen (Special)
    bool AuroraVeil = false,    // Aurora Veil — all categories, suppressed on crits
    bool Gravity    = false);   // Gravity — grounds all Pokemon, boosts Grav Apple

/// <summary>
/// All information needed to compute a single damage calculation.
/// AttackerStats and DefenderStats should already be computed at the desired investment level.
/// Optional fields default to the most common competitive scenario (no crits, no status, etc.).
/// </summary>
public record DamageContext(
    Pokemon AttackerSpecies,
    ComputedStats AttackerStats,
    string? AttackerAbility,
    string? AttackerItem,
    int AttackerStage,
    Pokemon DefenderSpecies,
    ComputedStats DefenderStats,        // .Def / .SpD / .Hp used
    int DefenderStage,
    Move Move,
    BattleState Battle,
    // ── Defender-side context ──
    string? DefenderAbility             = null,
    bool DefenderAtFullHp               = true,
    // ── Attacker status ──
    bool IsBurned                       = false,
    bool IsParalyzed                    = false,
    bool IsPoisoned                     = false,
    // ── Critical / random modifiers ──
    bool IsCritical                     = false,
    // ── Doubles / ally modifiers ──
    bool IsHelpingHand                  = false,    // ally used Helping Hand (Power chain, ×1.5)
    bool IsParentalBondSecondHit        = false,    // second hit of Parental Bond (×0.25)
    bool AllyHasFriendGuard             = false,    // ×0.75 on damage received (other chain)
    bool AllyHasBattery                 = false,    // ×1.3 special power (Power chain)
    bool AllyHasPowerSpot               = false,    // ×1.3 all power (Power chain)
    bool AllyHasSteellySpirit           = false,    // ×1.5 Steel power (Power chain)
    // ── Situational ──
    bool GlaiveRush                     = false,    // defender used Glaive Rush last turn (×2)
    bool TargetMovedFirst               = false,    // for Analytic (×1.3 power)
    bool IsCharged                      = false,    // Charge effect active; Electric moves ×2 power
    bool HasSheerForceBoost             = false,    // move has secondary effect eligible for Sheer Force
    int MetronomeCount                  = 0);       // consecutive Metronome item uses (1 = first use)

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
    ///   Damage = (floor(floor(22 × Power × A / D) / 50) + 2)
    ///            × Targets × PB × Weather × GlaiveRush × Critical
    ///            × random(85-100) × STAB × Type × Burn × other
    /// where Power is itself computed via a 4096-based chain (rounds half-up after each step,
    /// floors at the end), and "other" uses an identical chain.
    /// Each of the top-level modifiers (Targets through Burn) floors its result.
    /// </summary>
    public static DamageResult Calculate(DamageContext ctx)
    {
        var move = ctx.Move;
        bool isPhysical = move.Category == MoveCategory.Physical;
        bool isSpecial  = move.Category == MoveCategory.Special;

        if (move.Power is null or 0 || move.Category == MoveCategory.Status)
        {
            return new DamageResult(0, 0, ctx.DefenderStats.Hp,
                TypeChart.GetCombinedEffectiveness(move.Type, ctx.DefenderSpecies.Type1, ctx.DefenderSpecies.Type2),
                IsStab: false, 0, 0);
        }

        string? abilityId    = NormalizeId(ctx.AttackerAbility);
        string? itemId       = NormalizeId(ctx.AttackerItem);
        string? defAbilityId = NormalizeId(ctx.DefenderAbility);
        string  moveNormId   = NormalizeId(move.ShowdownId) ?? "";

        // ── Power (4096-based chain: PokeRound after each step, floor at end) ──
        int power = ComputePower(move.Power.Value, ctx, move, moveNormId,
                                 abilityId, itemId, defAbilityId, isPhysical, isSpecial);

        // ── Attack stat (stage + item + Guts) ────────────────────────────────
        int baseAtk      = isPhysical ? ctx.AttackerStats.Atk : ctx.AttackerStats.SpA;
        int effectiveAtk = ApplyStage(baseAtk, ctx.AttackerStage);

        // Guts: ×1.5 Attack when burned/paralyzed/poisoned (stat boost, not Power chain)
        if (isPhysical && abilityId is "guts" && (ctx.IsBurned || ctx.IsParalyzed || ctx.IsPoisoned))
            effectiveAtk = (int)Math.Floor(effectiveAtk * 1.5);

        if (isPhysical && itemId is "choiceband")   effectiveAtk = (int)Math.Floor(effectiveAtk * 1.5);
        if (!isPhysical && itemId is "choicespecs") effectiveAtk = (int)Math.Floor(effectiveAtk * 1.5);

        // ── Defense stat (stage) ──────────────────────────────────────────────
        int baseDef      = isPhysical ? ctx.DefenderStats.Def : ctx.DefenderStats.SpD;
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
        bool isSuperEffective   = typeEff > 1.0;
        bool isNotVeryEffective = typeEff > 0 && typeEff < 1.0;

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

        // ── Base damage (level 50): floor(floor(22 × Power × A / D) / 50) + 2 ─
        int inner   = (int)Math.Floor(22.0 * power * effectiveAtk / effectiveDef);
        int baseDmg = (int)Math.Floor(inner / 50.0) + 2;

        // ── 1. Targets (spread) ───────────────────────────────────────────────
        if (isSpread) baseDmg = (int)Math.Floor(baseDmg * 0.75);

        // ── 2. Parental Bond second hit (×0.25) ───────────────────────────────
        if (ctx.IsParentalBondSecondHit) baseDmg = (int)Math.Floor(baseDmg * 0.25);

        // ── 3. Weather ────────────────────────────────────────────────────────
        if (weatherMult != 1.0) baseDmg = (int)Math.Floor(baseDmg * weatherMult);

        // ── 4. Glaive Rush (×2 when defender used Glaive Rush last turn) ─────
        if (ctx.GlaiveRush) baseDmg *= 2;

        // ── 5. Critical hit (×1.5, Gen VI+) ──────────────────────────────────
        if (ctx.IsCritical) baseDmg = (int)Math.Floor(baseDmg * 1.5);

        // ── 6. Random rolls (85–100) ──────────────────────────────────────────
        int minDmg = (int)Math.Floor(baseDmg * 85.0 / 100.0);
        int maxDmg = baseDmg;

        // ── 7. STAB ───────────────────────────────────────────────────────────
        if (isStab)
        {
            double stabMult = isAdaptability ? 2.0 : 1.5;
            minDmg = (int)Math.Floor(minDmg * stabMult);
            maxDmg = (int)Math.Floor(maxDmg * stabMult);
        }

        // ── 8. Type effectiveness ─────────────────────────────────────────────
        if (typeEff != 1.0)
        {
            minDmg = (int)Math.Floor(minDmg * typeEff);
            maxDmg = (int)Math.Floor(maxDmg * typeEff);
        }

        // ── 9. Burn (physical; Guts negates; Facade negates when status-boosted) ──
        bool isFacadeBoosted = moveNormId == "facade" && (ctx.IsBurned || ctx.IsParalyzed || ctx.IsPoisoned);
        if (ctx.IsBurned && isPhysical && abilityId is not "guts" && !isFacadeBoosted)
        {
            minDmg = (int)Math.Floor(minDmg * 0.5);
            maxDmg = (int)Math.Floor(maxDmg * 0.5);
        }

        // ── 10. "other" chain ─────────────────────────────────────────────────
        minDmg = ApplyOtherChain(minDmg, ctx, move, abilityId, itemId, defAbilityId,
                                 isSuperEffective, isNotVeryEffective, isSpecial);
        maxDmg = ApplyOtherChain(maxDmg, ctx, move, abilityId, itemId, defAbilityId,
                                 isSuperEffective, isNotVeryEffective, isSpecial);

        minDmg = Math.Max(typeEff > 0 ? 1 : 0, minDmg);
        maxDmg = Math.Max(typeEff > 0 ? 1 : 0, maxDmg);

        return new DamageResult(
            minDmg, maxDmg, ctx.DefenderStats.Hp,
            typeEff, isStab, effectiveAtk, effectiveDef);
    }

    /// <summary>
    /// Computes the effective Power value used in the damage formula.
    /// Uses a 4096-based chain (PokeRound — rounds up at 0.5 — after each step).
    /// Final result: floor(basePower × chain / 4096).
    /// Modifier order follows the Gen IX order on Bulbapedia's Power page.
    /// </summary>
    private static int ComputePower(int basePower, DamageContext ctx, Move move, string moveNormId,
        string? abilityId, string? itemId, string? defAbilityId, bool isPhysical, bool isSpecial)
    {
        bool isContact  = move.Flags.Contains("contact");
        bool isSound    = move.Flags.Contains("sound");
        bool isPunch    = move.Flags.Contains("punch");
        bool isBite     = move.Flags.Contains("bite");
        bool isPulse    = move.Flags.Contains("pulse");
        bool isSlicing  = move.Flags.Contains("slicing");
        bool isSteel    = move.Type == PokemonType.Steel;
        bool isElectric = move.Type == PokemonType.Electric;
        bool isGrass    = move.Type == PokemonType.Grass;
        bool isPsychic  = move.Type == PokemonType.Psychic;
        bool isDragon   = move.Type == PokemonType.Dragon;
        bool isGround   = move.Type == PokemonType.Ground;
        bool isFire     = move.Type == PokemonType.Fire;
        bool isNormal   = move.Type == PokemonType.Normal;

        long chain = 4096;

        // ── Move-based modifiers ──────────────────────────────────────────────

        // Facade: ×2 when user is burned, paralyzed, or poisoned
        if (moveNormId == "facade" && (ctx.IsBurned || ctx.IsParalyzed || ctx.IsPoisoned))
            chain = PokeRound(chain, 8192);

        // Solar Beam / Solar Blade: ×0.5 in rain, sand, or snow
        if (moveNormId is "solarbeam" or "solarblade" &&
            ctx.Battle.Weather is DamageWeather.Rain or DamageWeather.Sand or DamageWeather.Snow)
            chain = PokeRound(chain, 2048);

        // Grav Apple: ×1.5 when Gravity is in effect
        if (moveNormId == "gravapple" && ctx.Battle.Gravity)
            chain = PokeRound(chain, 6144);

        // Helping Hand: ×1.5 from ally (Power modifier, not "other")
        if (ctx.IsHelpingHand)
            chain = PokeRound(chain, 6144);

        // Terrain power halves (target assumed grounded)
        if (ctx.Battle.Terrain == DamageTerrain.Grassy &&
            moveNormId is "earthquake" or "magnitude" or "bulldoze")
            chain = PokeRound(chain, 2048);     // ×0.5
        if (ctx.Battle.Terrain == DamageTerrain.Misty && isDragon)
            chain = PokeRound(chain, 2048);     // ×0.5

        // Terrain power boosts (attacker assumed grounded)
        if (ctx.Battle.Terrain == DamageTerrain.Electric && isElectric) chain = PokeRound(chain, 5325);
        else if (ctx.Battle.Terrain == DamageTerrain.Grassy && isGrass) chain = PokeRound(chain, 5325);
        else if (ctx.Battle.Terrain == DamageTerrain.Psychic && isPsychic) chain = PokeRound(chain, 5325);

        // Charge: ×2 for Electric moves
        if (ctx.IsCharged && isElectric)
            chain = PokeRound(chain, 8192);

        // ── Ability-based modifiers ───────────────────────────────────────────

        // Iron Fist: ×4915/4096 (~×1.2) for punching moves
        if (abilityId is "ironfist" && isPunch)
            chain = PokeRound(chain, 4915);

        // Analytic: ×5325/4096 (~×1.3) when target has already moved
        if (abilityId is "analytic" && ctx.TargetMovedFirst)
            chain = PokeRound(chain, 5325);

        // Sand Force: ×5325/4096 for Ground/Rock/Steel in sandstorm
        if (abilityId is "sandforce" && ctx.Battle.Weather == DamageWeather.Sand &&
            (isGround || move.Type == PokemonType.Rock || isSteel))
            chain = PokeRound(chain, 5325);

        // Sheer Force: ×5325/4096 for moves with a secondary effect (user must flag)
        if (abilityId is "sheerforce" && ctx.HasSheerForceBoost)
            chain = PokeRound(chain, 5325);

        // Tough Claws: ×5325/4096 for contact moves
        if (abilityId is "toughclaws" && isContact)
            chain = PokeRound(chain, 5325);

        // Battery (ally): ×5325/4096 for special moves
        if (ctx.AllyHasBattery && isSpecial)
            chain = PokeRound(chain, 5325);

        // Power Spot (ally): ×5325/4096 for all moves
        if (ctx.AllyHasPowerSpot)
            chain = PokeRound(chain, 5325);

        // Punk Rock (attacker): ×5325/4096 for sound moves
        if (abilityId is "punkrock" && isSound)
            chain = PokeRound(chain, 5325);

        // Strong Jaw: ×6144/4096 (×1.5) for biting moves
        if (abilityId is "strongjaw" && isBite)
            chain = PokeRound(chain, 6144);

        // Mega Launcher: ×6144/4096 (×1.5) for pulse moves
        if (abilityId is "megalauncher" && isPulse)
            chain = PokeRound(chain, 6144);

        // Technician: ×6144/4096 (×1.5) for moves with base power ≤ 60
        if (abilityId is "technician" && basePower <= 60)
            chain = PokeRound(chain, 6144);

        // Steely Spirit: ×6144/4096 (×1.5) for Steel moves; applies for each holder
        if (ctx.AllyHasSteellySpirit && isSteel)
            chain = PokeRound(chain, 6144);
        if (abilityId is "steelyspirit" && isSteel)
            chain = PokeRound(chain, 6144);

        // Sharpness: ×6144/4096 (×1.5) for slicing moves
        if (abilityId is "sharpness" && isSlicing)
            chain = PokeRound(chain, 6144);

        // Dry Skin (defender): ×5120/4096 (~×1.25) for Fire-type moves
        if (defAbilityId is "dryskin" && isFire)
            chain = PokeRound(chain, 5120);

        // ── Item-based modifiers ──────────────────────────────────────────────

        // Muscle Band (physical) / Wise Glasses (special): ×4505/4096 (~×1.1)
        if (isPhysical && itemId is "muscleband") chain = PokeRound(chain, 4505);
        if (isSpecial  && itemId is "wiseglasses") chain = PokeRound(chain, 4505);

        // Type-enhancing items (Plates, type items, Incenses): ×4915/4096 (~×1.2)
        if (itemId is not null && TypeEnhancingItems.TryGetValue(itemId, out var enhancedType) &&
            move.Type == enhancedType)
            chain = PokeRound(chain, 4915);

        // Normal Gem: ×5325/4096 (~×1.3) for Normal-type moves
        if (itemId is "normalgem" && isNormal)
            chain = PokeRound(chain, 5325);

        // Punching Glove: ×4506/4096 (~×1.1) for punching moves
        if (isPunch && itemId is "punchingglove")
            chain = PokeRound(chain, 4506);

        if (chain == 4096) return basePower;
        return (int)Math.Floor((double)basePower * chain / 4096);
    }

    /// <summary>
    /// Computes and applies the "other" modifier block using a 4096-based fixed-point chain.
    /// Each factor multiplies the running chain, rounding half-up after each step.
    /// Final result: floor(dmg × chain / 4096).
    /// Order follows Bulbapedia's Gen V+ damage formula "other" table.
    /// </summary>
    private static int ApplyOtherChain(int dmg, DamageContext ctx, Move move,
        string? abilityId, string? itemId, string? defAbilityId,
        bool isSE, bool isNVE, bool isSpecial)
    {
        bool isContact  = move.Flags.Contains("contact");
        bool isSound    = move.Flags.Contains("sound");
        bool isFireType = move.Type == PokemonType.Fire;

        long chain = 4096;

        // Reflect / Light Screen — suppressed on crits
        if (ctx.Battle.Screens && !ctx.IsCritical)
            chain = PokeRound(chain, 2048);     // ×0.5

        // Aurora Veil — suppressed on crits; covers all categories
        if (ctx.Battle.AuroraVeil && !ctx.IsCritical)
            chain = PokeRound(chain, 2048);     // ×0.5

        // Multiscale / Shadow Shield — ×0.5 when at full HP
        if (defAbilityId is "multiscale" or "shadowshield" && ctx.DefenderAtFullHp)
            chain = PokeRound(chain, 2048);     // ×0.5

        // Fluffy — contact moves ×0.5
        if (defAbilityId is "fluffy" && isContact)
            chain = PokeRound(chain, 2048);     // ×0.5

        // Punk Rock (defensive) — sound moves ×0.5
        if (defAbilityId is "punkrock" && isSound)
            chain = PokeRound(chain, 2048);     // ×0.5

        // Ice Scales — special moves ×0.5
        if (defAbilityId is "icescales" && isSpecial)
            chain = PokeRound(chain, 2048);     // ×0.5

        // Friend Guard — ×0.75 from ally ability
        if (ctx.AllyHasFriendGuard)
            chain = PokeRound(chain, 3072);     // ×0.75

        // Filter / Solid Rock / Prism Armor — ×0.75 vs super-effective
        if (defAbilityId is "filter" or "solidrock" or "prismarmor" && isSE)
            chain = PokeRound(chain, 3072);     // ×0.75

        // Neuroforce — ×1.25 vs super-effective
        if (abilityId is "neuroforce" && isSE)
            chain = PokeRound(chain, 5120);     // ×1.25

        // Sniper — ×1.5 on critical hits (stacks with the ×1.5 critical step)
        if (abilityId is "sniper" && ctx.IsCritical)
            chain = PokeRound(chain, 6144);     // ×1.5

        // Tinted Lens — ×2 vs not-very-effective
        if (abilityId is "tintedlens" && isNVE)
            chain = PokeRound(chain, 8192);     // ×2

        // Fluffy (Fire) — fire-type moves ×2
        if (defAbilityId is "fluffy" && isFireType)
            chain = PokeRound(chain, 8192);     // ×2

        // Expert Belt — ×4915/4096 (~×1.2) vs super-effective
        if (itemId is "expertbelt" && isSE)
            chain = PokeRound(chain, 4915);

        // Life Orb — ×5324/4096 (~×1.3)
        if (itemId is "lifeorb")
            chain = PokeRound(chain, 5324);

        // Metronome item — +20% per consecutive use, capped at ×2 (5+ uses)
        if (itemId is "metronome" && ctx.MetronomeCount >= 1)
        {
            ReadOnlySpan<long> metroNumerators = [4915, 5734, 6554, 7373, 8192];
            chain = PokeRound(chain, metroNumerators[Math.Min(ctx.MetronomeCount, 5) - 1]);
        }

        if (chain == 4096) return dmg;
        return (int)Math.Floor(dmg * chain / 4096.0);
    }

    // Rounds half-up: equivalent to MidpointRounding.AwayFromZero for positive values.
    private static long PokeRound(long chain, long numerator)
        => (long)Math.Floor((double)chain * numerator / 4096 + 0.5);

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

    // Type-enhancing items: maps normalized Showdown item ID → the type they boost (×4915/4096).
    // Includes Plates, type-specific held items, and Incenses.
    private static readonly Dictionary<string, PokemonType> TypeEnhancingItems =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // Plates
        ["flameplate"]   = PokemonType.Fire,
        ["splashplate"]  = PokemonType.Water,
        ["zapplate"]     = PokemonType.Electric,
        ["meadowplate"]  = PokemonType.Grass,
        ["icicleplate"]  = PokemonType.Ice,
        ["fistplate"]    = PokemonType.Fighting,
        ["toxicplate"]   = PokemonType.Poison,
        ["earthplate"]   = PokemonType.Ground,
        ["skyplate"]     = PokemonType.Flying,
        ["spookyplate"]  = PokemonType.Ghost,
        ["mindplate"]    = PokemonType.Psychic,
        ["insectplate"]  = PokemonType.Bug,
        ["stoneplate"]   = PokemonType.Rock,
        ["dreadplate"]   = PokemonType.Dark,
        ["dracoplate"]   = PokemonType.Dragon,
        ["ironplate"]    = PokemonType.Steel,
        ["pixieplate"]   = PokemonType.Fairy,
        // Type-enhancing held items
        ["charcoal"]     = PokemonType.Fire,
        ["mysticwater"]  = PokemonType.Water,
        ["magnet"]       = PokemonType.Electric,
        ["miracleseed"]  = PokemonType.Grass,
        ["nevermeltice"] = PokemonType.Ice,
        ["blackbelt"]    = PokemonType.Fighting,
        ["poisonbarb"]   = PokemonType.Poison,
        ["softsand"]     = PokemonType.Ground,
        ["sharpbeak"]    = PokemonType.Flying,
        ["spelltag"]     = PokemonType.Ghost,
        ["twistedspoon"] = PokemonType.Psychic,
        ["oddincense"]   = PokemonType.Psychic,
        ["silverpowder"] = PokemonType.Bug,
        ["hardstone"]    = PokemonType.Rock,
        ["blackglasses"] = PokemonType.Dark,
        ["dragonfang"]   = PokemonType.Dragon,
        ["metalcoat"]    = PokemonType.Steel,
        ["silkscarf"]    = PokemonType.Normal,
        ["fairyfeather"] = PokemonType.Fairy,
        // Incenses
        ["seaincense"]   = PokemonType.Water,
        ["waveincense"]  = PokemonType.Water,
        ["roseincense"]  = PokemonType.Grass,
        ["rockincense"]  = PokemonType.Rock,
        ["luckincense"]  = PokemonType.Normal,
    };
}
