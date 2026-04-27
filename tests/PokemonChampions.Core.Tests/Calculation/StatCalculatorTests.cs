using PokemonChampions.Core.Calculation;
using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Enums;
using Xunit;

namespace PokemonChampions.Core.Tests.Calculation;

/// <summary>
/// All expected values computed by hand from the standard Gen 9 level-50 formulas.
/// Stat points feed into the EV slot as ev = statPoints * 8; nature amplifies them.
/// HP:    floor((2*base + iv + floor(ev/4)) * 50/100) + 60
/// Other: floor((floor((2*base + iv + floor(ev/4)) * 50/100) + 5) * natureMult)
///        where ev = statPoints * 8
/// </summary>
public class StatCalculatorTests
{
    // -- HP formula (underlying, no stat points) ----------------------------

    [Theory]
    // (baseStat, iv, ev, expected)
    // floor((2*base + iv + floor(ev/4)) * 0.5) + 60
    [InlineData(78,  31,   0, 153)] // Charizard HP, 0 EVs:  floor((156+31)*0.5)+60 = floor(93.5)+60 = 93+60 = 153
    [InlineData(78,  31, 252, 185)] // Charizard HP, max:    floor((156+31+63)*0.5)+60 = floor(125)+60 = 185
    [InlineData(45,  31,   0, 120)] // base-45 HP, 0 EVs:    floor((90+31)*0.5)+60 = floor(60.5)+60 = 120
    [InlineData(100, 31, 252, 207)] // base-100 HP, max EVs: floor((200+31+63)*0.5)+60 = 147+60 = 207
    public void CalculateHp_StandardValues_MatchesExpectedStats(int baseStat, int iv, int ev, int expected)
    {
        var result = StatCalculator.CalculateHp(baseStat, iv, ev);
        Assert.Equal(expected, result);
    }

    // -- Non-HP stat formula (underlying, no stat points) --------------------

    [Theory]
    // (baseStat, iv, ev, natureMult, expected)
    // floor((floor((2*base + iv + floor(ev/4)) * 0.5) + 5) * natureMult)
    [InlineData(90,  31,   0, 1.0, 110)] // base-90, neutral, no EVs:   floor((105+5)*1.0) = 110
    [InlineData(90,  31,   0, 0.9,  99)] // base-90, hindering:          floor(110*0.9) = floor(99) = 99
    [InlineData(90,  31, 252, 1.1, 156)] // base-90, boosting, max EVs: floor(142*1.1) = floor(156.2) = 156
    [InlineData(115, 31, 252, 1.1, 183)] // Incineroar Atk, adamant, max EVs:
                                          // floor((floor((230+31+63)*0.5)+5)*1.1) = floor((162+5)*1.1) = floor(183.7) = 183
    public void CalculateStat_StandardValues_MatchesExpectedStats(int baseStat, int iv, int ev, double natureMult, int expected)
    {
        var result = StatCalculator.CalculateStat(baseStat, iv, ev, natureMult);
        Assert.Equal(expected, result);
    }

    // -- GetRange ------------------------------------------------------------

    [Fact]
    public void GetRange_HpStat_ReturnsCorrectMinMax()
    {
        // Incineroar: base HP 95
        var range = StatCalculator.GetRange(95, StatName.Hp);
        Assert.Equal(95, range.Base);
        // Min: 31 IV, 0 stat points → floor((190+31)*0.5)+60 = floor(110.5)+60 = 170
        Assert.Equal(170, range.Min);
        // Max: 31 IV, 32 stat points added directly → 170 + 32 = 202
        Assert.Equal(202, range.Max);
    }

    [Fact]
    public void GetRange_NonHpStat_ReturnsCorrectMinMax()
    {
        // base-90 Speed (e.g. Charizard)
        var range = StatCalculator.GetRange(90, StatName.Spe);
        Assert.Equal(90, range.Base);
        // Min: 31 IV, 0 stat points, hindering (×0.9)
        // floor((floor((180+31+0)*0.5)+5)*0.9) = floor(110*0.9) = 99
        Assert.Equal(99, range.Min);
        // Max: 31 IV, 32 stat points (ev=256), boosting (×1.1)
        // floor((floor((180+31+64)*0.5)+5)*1.1) = floor((137+5)*1.1) = floor(156.2) = 156
        Assert.Equal(156, range.Max);
    }

    [Fact]
    public void GetRange_HpStatHasNoNatureModifier()
    {
        // HP formula has no nature multiplier; min = 0 stat points result.
        var range = StatCalculator.GetRange(50, StatName.Hp);
        int directMin = StatCalculator.CalculateHp(50, AppConstants.MaxIv, 0);
        Assert.Equal(directMin, range.Min);
    }

    [Fact]
    public void GetRange_MaxIsExactlyMinPlusMaxStatPoints_ForHp()
    {
        // Stat points add exactly 1 each for HP (no nature, no formula involvement).
        var range = StatCalculator.GetRange(100, StatName.Hp);
        Assert.Equal(range.Min + AppConstants.MaxStatPointsPerStat, range.Max);
    }

    // -- Compute (full instance stats) ---------------------------------------

    [Fact]
    public void Compute_AdamantIncineroar_32AtkStatPoints_ProducesExpectedAtk()
    {
        var baseStats = new BaseStats(Hp: 95, Atk: 115, Def: 90, SpA: 80, SpD: 90, Spe: 60);
        // 32 stat points in Atk with Adamant (+10%); ev = 32*8 = 256
        // floor((floor((230+31+64)*0.5)+5)*1.1) = floor((162+5)*1.1) = floor(183.7) = 183
        var spread = new EvSpread(Hp: 0, Atk: 32, Def: 0, SpA: 0, SpD: 0, Spe: 0);
        var ivs = IvSpread.Perfect;
        var nature = Nature.TryGet("adamant")!;

        var stats = StatCalculator.Compute(baseStats, spread, ivs, nature);

        Assert.Equal(183, stats.Atk);
    }

    [Fact]
    public void Compute_StatPointsAreAmplifiedByNature()
    {
        // Stat points feed into the EV slot and are amplified by nature.
        // With base Spe 60, Timid (+10%), 32 stat points (ev=256):
        // floor((floor((120+31+64)*0.5)+5)*1.1) = floor((107+5)*1.1) = floor(123.2) = 123
        var baseStats = new BaseStats(Hp: 95, Atk: 115, Def: 90, SpA: 80, SpD: 90, Spe: 60);
        var spread = new EvSpread(Hp: 0, Atk: 0, Def: 0, SpA: 0, SpD: 0, Spe: 32);
        var nature = Nature.TryGet("timid")!;

        var stats = StatCalculator.Compute(baseStats, spread, IvSpread.Perfect, nature);

        Assert.Equal(123, stats.Spe);
    }

    [Fact]
    public void Compute_NeutralNature_ZeroStatPoints_MatchesBaseFormula()
    {
        var baseStats = new BaseStats(Hp: 45, Atk: 49, Def: 49, SpA: 65, SpD: 65, Spe: 45);
        var nature = Nature.TryGet("serious")!; // neutral

        var stats = StatCalculator.Compute(baseStats, EvSpread.Zero, IvSpread.Perfect, nature);

        // With 0 stat points, Compute must equal the raw formula results exactly.
        Assert.Equal(StatCalculator.CalculateHp(45, 31, 0),        stats.Hp);
        Assert.Equal(StatCalculator.CalculateStat(49, 31, 0, 1.0),  stats.Atk);
        Assert.Equal(StatCalculator.CalculateStat(65, 31, 0, 1.0),  stats.SpA);
        Assert.Equal(StatCalculator.CalculateStat(45, 31, 0, 1.0),  stats.Spe);
    }

    [Fact]
    public void Compute_HpStatPoints_Add32ToFinalHp()
    {
        // HP has no nature modifier; 32 stat points (ev=256) add exactly 32 to HP at level 50.
        var baseStats = new BaseStats(Hp: 95, Atk: 115, Def: 90, SpA: 80, SpD: 90, Spe: 60);
        var spread32 = new EvSpread(Hp: 32, Atk: 0, Def: 0, SpA: 0, SpD: 0, Spe: 0);
        var spread0  = EvSpread.Zero;
        var nature = Nature.TryGet("serious")!;

        var statsWithSps = StatCalculator.Compute(baseStats, spread32, IvSpread.Perfect, nature);
        var statsWithout = StatCalculator.Compute(baseStats, spread0,  IvSpread.Perfect, nature);

        Assert.Equal(statsWithout.Hp + 32, statsWithSps.Hp);
    }

    // -- GetAllRanges --------------------------------------------------------

    [Fact]
    public void GetAllRanges_ReturnsSixStats()
    {
        var baseStats = new BaseStats(50, 50, 50, 50, 50, 50);
        var ranges = StatCalculator.GetAllRanges(baseStats);
        Assert.Equal(6, ranges.Count);
        Assert.True(ranges.ContainsKey(StatName.Hp));
        Assert.True(ranges.ContainsKey(StatName.Spe));
    }

    [Fact]
    public void GetAllRanges_MinAlwaysLessThanOrEqualToMax()
    {
        var baseStats = new BaseStats(100, 100, 100, 100, 100, 100);
        var ranges = StatCalculator.GetAllRanges(baseStats);
        foreach (var (stat, range) in ranges)
            Assert.True(range.Min <= range.Max, $"{stat}: min ({range.Min}) > max ({range.Max})");
    }
}
