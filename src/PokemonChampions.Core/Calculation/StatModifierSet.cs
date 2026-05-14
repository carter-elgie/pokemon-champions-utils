using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Calculation;

/// <summary>
/// Represents stat modifiers (stages, field effects, abilities, items) applied to one computed stat.
/// All multipliers are stat-aware: call <see cref="Apply"/>, <see cref="HasAny"/>, and
/// <see cref="Describe"/> with the target <see cref="StatName"/> so only relevant modifiers fire.
/// Pokemon-specific items (Eviolite, Light Ball, Thick Club) are also validated against the
/// provided <see cref="Pokemon"/> — they are skipped if the Pokemon cannot benefit.
/// </summary>
public record StatModifierSet(
    int  Stage             = 0,   // −6 to +6 stat-stage boost (applies to every stat)

    // ── Speed ────────────────────────────────────────────────────────────────
    bool HasScarf          = false, // Choice Scarf ×1.5
    bool HasTailwind       = false, // Tailwind ×2
    bool HasParalysis      = false, // Paralysis ×0.5
    bool HasChlorophyll    = false, // Chlorophyll ×2 (sun assumed)
    bool HasSwiftSwim      = false, // Swift Swim ×2 (rain assumed)
    bool HasSandRush       = false, // Sand Rush ×2 (sand assumed)
    bool HasSlushRush      = false, // Slush Rush ×2 (snow assumed)
    bool HasUnburden       = false, // Unburden ×2 (item consumed assumed)
    bool HasSurgeSurfer    = false, // Surge Surfer ×2 (electric terrain assumed)
    bool HasQuickFeet      = false, // Quick Feet ×1.5 (statused assumed)
    bool HasSlowStart      = false, // Slow Start ×0.5
    bool HasIronBall       = false, // Iron Ball ×0.5

    // ── Attack ───────────────────────────────────────────────────────────────
    bool HasChoiceBand     = false, // Choice Band ×1.5
    bool HasHugePower      = false, // Huge Power / Pure Power ×2
    bool HasHustle         = false, // Hustle ×1.5
    bool HasGorillaTactics = false, // Gorilla Tactics ×1.5
    bool HasGuts           = false, // Guts ×1.5 (statused assumed)
    bool HasDefeatist      = false, // Defeatist ×0.5 (low HP assumed)
    bool HasFlowerGift     = false, // Flower Gift ×1.5 Atk (sun assumed)
    bool HasLightBall      = false, // Light Ball ×2 Atk+SpA (Pikachu family only)
    bool HasThickClub      = false, // Thick Club ×2 Atk (Cubone/Marowak only)

    // ── Special Attack ────────────────────────────────────────────────────────
    bool HasChoiceSpecs    = false, // Choice Specs ×1.5
    bool HasSolarPower     = false, // Solar Power ×1.5 (sun assumed)
    bool HasPlus           = false, // Plus ×1.5 (ally has Minus assumed)
    bool HasMinus          = false, // Minus ×1.5 (ally has Plus assumed)
    bool HasHadronEngine   = false, // Hadron Engine ×4/3

    // ── Defense ──────────────────────────────────────────────────────────────
    bool HasFurCoat        = false, // Fur Coat ×2
    bool HasMarvelScale    = false, // Marvel Scale ×1.5 (statused assumed)
    bool HasEviolite       = false, // Eviolite ×1.5 Def+SpD (non-fully-evolved only)

    // ── Special Defense ───────────────────────────────────────────────────────
    bool HasIceScales      = false, // Ice Scales ×2
    bool HasAssaultVest    = false  // Assault Vest ×1.5
)
{
    public static readonly StatModifierSet None = new();

    // ── Core stat-aware API ───────────────────────────────────────────────────

    /// <summary>True if any modifier affects <paramref name="stat"/> for <paramref name="pokemon"/>.</summary>
    public bool HasAny(StatName stat, Pokemon? pokemon = null)
        => Math.Abs(TotalMultiplier(stat, pokemon) - 1.0) > 1e-9;

    /// <summary>Combined multiplier for <paramref name="stat"/>; floor is applied in <see cref="Apply"/>.</summary>
    public double TotalMultiplier(StatName stat, Pokemon? pokemon = null)
    {
        double mult = 1.0;

        // Stage applies to every stat
        if (Stage > 0) mult *= (2.0 + Stage) / 2.0;
        else if (Stage < 0) mult *= 2.0 / (2.0 - Stage);

        switch (stat)
        {
            case StatName.Spe:
                if (HasScarf)       mult *= 1.5;
                if (HasTailwind)    mult *= 2.0;
                if (HasParalysis)   mult *= 0.5;
                if (HasChlorophyll) mult *= 2.0;
                if (HasSwiftSwim)   mult *= 2.0;
                if (HasSandRush)    mult *= 2.0;
                if (HasSlushRush)   mult *= 2.0;
                if (HasUnburden)    mult *= 2.0;
                if (HasSurgeSurfer) mult *= 2.0;
                if (HasQuickFeet)   mult *= 1.5;
                if (HasSlowStart)   mult *= 0.5;
                if (HasIronBall)    mult *= 0.5;
                break;

            case StatName.Atk:
                if (HasChoiceBand)                                mult *= 1.5;
                if (HasHugePower)                                 mult *= 2.0;
                if (HasHustle)                                    mult *= 1.5;
                if (HasGorillaTactics)                            mult *= 1.5;
                if (HasGuts)                                      mult *= 1.5;
                if (HasDefeatist)                                 mult *= 0.5;
                if (HasFlowerGift)                                mult *= 1.5;
                if (HasLightBall  && IsLightBallPokemon(pokemon)) mult *= 2.0;
                if (HasThickClub  && IsThickClubPokemon(pokemon)) mult *= 2.0;
                break;

            case StatName.SpA:
                if (HasChoiceSpecs)                               mult *= 1.5;
                if (HasSolarPower)                                mult *= 1.5;
                if (HasPlus)                                      mult *= 1.5;
                if (HasMinus)                                     mult *= 1.5;
                if (HasHadronEngine)                              mult *= 4.0 / 3.0;
                if (HasDefeatist)                                 mult *= 0.5;
                if (HasLightBall  && IsLightBallPokemon(pokemon)) mult *= 2.0;
                break;

            case StatName.Def:
                if (HasFurCoat)                                   mult *= 2.0;
                if (HasMarvelScale)                               mult *= 1.5;
                if (HasEviolite   && IsEvioliteValid(pokemon))    mult *= 1.5;
                break;

            case StatName.SpD:
                if (HasIceScales)                                 mult *= 2.0;
                if (HasAssaultVest)                               mult *= 1.5;
                if (HasEviolite   && IsEvioliteValid(pokemon))    mult *= 1.5;
                break;

            case StatName.Hp:
                break; // no standard multipliers for HP
        }

        return mult;
    }

    /// <summary>Applies all modifiers for <paramref name="stat"/> to <paramref name="statValue"/>, flooring the result.</summary>
    public int Apply(int statValue, StatName stat, Pokemon? pokemon = null)
    {
        var mult = TotalMultiplier(stat, pokemon);
        return Math.Abs(mult - 1.0) < 1e-9 ? statValue : (int)Math.Floor(statValue * mult);
    }

    /// <summary>Human-readable description of active modifiers for <paramref name="stat"/>.</summary>
    public string Describe(StatName stat, Pokemon? pokemon = null)
    {
        var parts = new List<string>();
        if (Stage > 0) parts.Add($"+{Stage}");
        else if (Stage < 0) parts.Add(Stage.ToString());

        switch (stat)
        {
            case StatName.Spe:
                if (HasScarf)       parts.Add("Choice Scarf");
                if (HasTailwind)    parts.Add("Tailwind");
                if (HasParalysis)   parts.Add("Paralysis");
                if (HasChlorophyll) parts.Add("Chlorophyll");
                if (HasSwiftSwim)   parts.Add("Swift Swim");
                if (HasSandRush)    parts.Add("Sand Rush");
                if (HasSlushRush)   parts.Add("Slush Rush");
                if (HasUnburden)    parts.Add("Unburden");
                if (HasSurgeSurfer) parts.Add("Surge Surfer");
                if (HasQuickFeet)   parts.Add("Quick Feet");
                if (HasSlowStart)   parts.Add("Slow Start");
                if (HasIronBall)    parts.Add("Iron Ball");
                break;
            case StatName.Atk:
                if (HasChoiceBand)                                parts.Add("Choice Band");
                if (HasHugePower)                                 parts.Add("Huge/Pure Power");
                if (HasHustle)                                    parts.Add("Hustle");
                if (HasGorillaTactics)                            parts.Add("Gorilla Tactics");
                if (HasGuts)                                      parts.Add("Guts");
                if (HasDefeatist)                                 parts.Add("Defeatist");
                if (HasFlowerGift)                                parts.Add("Flower Gift");
                if (HasLightBall  && IsLightBallPokemon(pokemon)) parts.Add("Light Ball");
                if (HasThickClub  && IsThickClubPokemon(pokemon)) parts.Add("Thick Club");
                break;
            case StatName.SpA:
                if (HasChoiceSpecs)                               parts.Add("Choice Specs");
                if (HasSolarPower)                                parts.Add("Solar Power");
                if (HasPlus)                                      parts.Add("Plus");
                if (HasMinus)                                     parts.Add("Minus");
                if (HasHadronEngine)                              parts.Add("Hadron Engine");
                if (HasDefeatist)                                 parts.Add("Defeatist");
                if (HasLightBall  && IsLightBallPokemon(pokemon)) parts.Add("Light Ball");
                break;
            case StatName.Def:
                if (HasFurCoat)                                   parts.Add("Fur Coat");
                if (HasMarvelScale)                               parts.Add("Marvel Scale");
                if (HasEviolite   && IsEvioliteValid(pokemon))    parts.Add("Eviolite");
                break;
            case StatName.SpD:
                if (HasIceScales)                                 parts.Add("Ice Scales");
                if (HasAssaultVest)                               parts.Add("Assault Vest");
                if (HasEviolite   && IsEvioliteValid(pokemon))    parts.Add("Eviolite");
                break;
        }

        return string.Join(", ", parts);
    }

    // ── Parsing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses modifier tokens from the tail of a user command.
    /// Handles single-word tokens (e.g. "scarf", "chlorophyll") and two-word tokens
    /// (e.g. "choice band", "assault vest", "huge power"). Hyphens in single tokens
    /// are ignored so "choice-band" also matches.
    /// </summary>
    public static StatModifierSet Parse(string[] tokens)
    {
        int    stage          = 0;
        bool   scarf          = false;
        bool   tailwind       = false;
        bool   paralysis      = false;
        bool   chlorophyll    = false;
        bool   swiftSwim      = false;
        bool   sandRush       = false;
        bool   slushRush      = false;
        bool   unburden       = false;
        bool   surgeSurfer    = false;
        bool   quickFeet      = false;
        bool   slowStart      = false;
        bool   ironBall       = false;
        bool   choiceBand     = false;
        bool   hugePower      = false;
        bool   hustle         = false;
        bool   gorillaTactics = false;
        bool   guts           = false;
        bool   defeatist      = false;
        bool   flowerGift     = false;
        bool   lightBall      = false;
        bool   thickClub      = false;
        bool   choiceSpecs    = false;
        bool   solarPower     = false;
        bool   plus           = false;
        bool   minus          = false;
        bool   hadronEngine   = false;
        bool   furCoat        = false;
        bool   marvelScale    = false;
        bool   eviolite       = false;
        bool   iceScales      = false;
        bool   assaultVest    = false;

        int i = 0;
        while (i < tokens.Length)
        {
            var t    = tokens[i].ToLowerInvariant().Trim();
            var next = i + 1 < tokens.Length ? tokens[i + 1].ToLowerInvariant().Trim() : null;
            var two  = next is not null ? t + " " + next : null;

            // Two-word matches (checked first to avoid partial single-word grabs)
            if (two is not null)
            {
                switch (two)
                {
                    case "choice scarf":    scarf          = true; i += 2; continue;
                    case "choice band":     choiceBand     = true; i += 2; continue;
                    case "choice specs":    choiceSpecs    = true; i += 2; continue;
                    case "assault vest":    assaultVest    = true; i += 2; continue;
                    case "iron ball":       ironBall       = true; i += 2; continue;
                    case "fur coat":        furCoat        = true; i += 2; continue;
                    case "huge power":      hugePower      = true; i += 2; continue;
                    case "pure power":      hugePower      = true; i += 2; continue;
                    case "gorilla tactics": gorillaTactics = true; i += 2; continue;
                    case "quick feet":      quickFeet      = true; i += 2; continue;
                    case "swift swim":      swiftSwim      = true; i += 2; continue;
                    case "sand rush":       sandRush       = true; i += 2; continue;
                    case "slush rush":      slushRush      = true; i += 2; continue;
                    case "hadron engine":   hadronEngine   = true; i += 2; continue;
                    case "solar power":     solarPower     = true; i += 2; continue;
                    case "flower gift":     flowerGift     = true; i += 2; continue;
                    case "marvel scale":    marvelScale    = true; i += 2; continue;
                    case "ice scales":      iceScales      = true; i += 2; continue;
                    case "slow start":      slowStart      = true; i += 2; continue;
                    case "light ball":      lightBall      = true; i += 2; continue;
                    case "thick club":      thickClub      = true; i += 2; continue;
                    case "surge surfer":    surgeSurfer    = true; i += 2; continue;
                }
            }

            // Stage modifier (+N / −N)
            if ((t.StartsWith('+') || t.StartsWith('-')) && int.TryParse(t, out int n) && n != 0)
            {
                stage = Math.Clamp(stage + n, -6, 6);
                i++;
                continue;
            }

            // Single-word matches (normalize away hyphens/underscores)
            var norm = t.Replace("-", "").Replace("_", "");
            switch (norm)
            {
                case "scarf":
                case "choicescarf":      scarf          = true; break;
                case "tailwind":
                case "tw":               tailwind       = true; break;
                case "paralysis":
                case "para":             paralysis      = true; break;
                case "band":
                case "choiceband":       choiceBand     = true; break;
                case "specs":
                case "choicespecs":      choiceSpecs    = true; break;
                case "vest":
                case "assaultvest":      assaultVest    = true; break;
                case "ironball":         ironBall       = true; break;
                case "furcoat":          furCoat        = true; break;
                case "hugepower":
                case "purepower":        hugePower      = true; break;
                case "gorillatactics":   gorillaTactics = true; break;
                case "quickfeet":        quickFeet      = true; break;
                case "swiftswim":        swiftSwim      = true; break;
                case "sandrush":         sandRush       = true; break;
                case "slushrush":        slushRush      = true; break;
                case "chlorophyll":      chlorophyll    = true; break;
                case "unburden":         unburden       = true; break;
                case "surgesurfer":      surgeSurfer    = true; break;
                case "slowstart":        slowStart      = true; break;
                case "hustle":           hustle         = true; break;
                case "guts":             guts           = true; break;
                case "defeatist":        defeatist      = true; break;
                case "flowergift":       flowerGift     = true; break;
                case "lightball":        lightBall      = true; break;
                case "thickclub":        thickClub      = true; break;
                case "hadronengine":     hadronEngine   = true; break;
                case "solarpower":       solarPower     = true; break;
                case "plus":             plus           = true; break;
                case "minus":            minus          = true; break;
                case "marvelscale":      marvelScale    = true; break;
                case "icescales":        iceScales      = true; break;
                case "eviolite":         eviolite       = true; break;
            }
            i++;
        }

        return new StatModifierSet(
            stage,
            scarf, tailwind, paralysis,
            chlorophyll, swiftSwim, sandRush, slushRush, unburden, surgeSurfer, quickFeet, slowStart, ironBall,
            choiceBand, hugePower, hustle, gorillaTactics, guts, defeatist, flowerGift, lightBall, thickClub,
            choiceSpecs, solarPower, plus, minus, hadronEngine,
            furCoat, marvelScale, eviolite,
            iceScales, assaultVest);
    }

    // ── Pokemon-specific item validation ──────────────────────────────────────

    // Light Ball doubles Atk and SpA for Pikachu and its cosplay/cap/partner variants.
    private static bool IsLightBallPokemon(Pokemon? pokemon)
        => pokemon is not null && pokemon.ShowdownId.StartsWith("pikachu");

    // Thick Club doubles Atk for Cubone and all Marowak forms.
    private static bool IsThickClubPokemon(Pokemon? pokemon)
        => pokemon is not null
           && (pokemon.ShowdownId == "cubone" || pokemon.ShowdownId.StartsWith("marowak"));

    // Eviolite boosts Def and SpD ×1.5 only for Pokemon that can still evolve.
    private static bool IsEvioliteValid(Pokemon? pokemon)
        => pokemon is not null && pokemon.CanEvolve;
}
