namespace PokemonChampions.Core.Calculation;

/// <summary>
/// Represents one or more stat modifiers (stage boosts, held items, field effects)
/// that are applied multiplicatively to a computed stat value.
/// All multipliers are combined and the result is floored once.
/// </summary>
public record StatModifierSet(
    int Stage = 0,            // -6 to +6 stat stage boost
    bool HasScarf = false,    // Choice Scarf ×1.5 (speed only)
    bool HasTailwind = false, // Tailwind ×2 (speed only)
    bool HasParalysis = false // Paralysis ×0.5 (speed only)
)
{
    public static readonly StatModifierSet None = new();

    public bool HasAny => Stage != 0 || HasScarf || HasTailwind || HasParalysis;

    public double TotalMultiplier
    {
        get
        {
            double mult = 1.0;
            if (Stage > 0) mult *= (2.0 + Stage) / 2.0;
            else if (Stage < 0) mult *= 2.0 / (2.0 - Stage); // Stage is negative, so 2-Stage > 2
            if (HasScarf)     mult *= 1.5;
            if (HasTailwind)  mult *= 2.0;
            if (HasParalysis) mult *= 0.5;
            return mult;
        }
    }

    /// <summary>Applies all modifiers to a computed stat, flooring the result.</summary>
    public int Apply(int stat) => HasAny ? (int)Math.Floor(stat * TotalMultiplier) : stat;

    /// <summary>Human-readable label for active modifiers, e.g. "Choice Scarf, +2".</summary>
    public string Describe()
    {
        var parts = new List<string>();
        if (Stage > 0) parts.Add($"+{Stage}");
        else if (Stage < 0) parts.Add(Stage.ToString());
        if (HasScarf) parts.Add("Choice Scarf");
        if (HasTailwind) parts.Add("Tailwind");
        if (HasParalysis) parts.Add("Paralysis");
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Parses modifier tokens from the tail of a user command.
    /// Recognised tokens: +N / -N (stage, clamped to ±6), "scarf", "tailwind"/"tw", "paralysis"/"para".
    /// Unrecognised tokens are silently ignored.
    /// </summary>
    public static StatModifierSet Parse(string[] tokens)
    {
        int stage = 0;
        bool scarf = false, tailwind = false, paralysis = false;

        foreach (var raw in tokens)
        {
            var t = raw.ToLowerInvariant().Trim();

            if ((t.StartsWith('+') || t.StartsWith('-')) && int.TryParse(t, out int n) && n != 0)
            {
                stage = Math.Clamp(stage + n, -6, 6);
                continue;
            }

            switch (t)
            {
                case "scarf":
                case "choicescarf":
                    scarf = true; break;
                case "tailwind":
                case "tw":
                    tailwind = true; break;
                case "paralysis":
                case "para":
                    paralysis = true; break;
            }
        }

        return new StatModifierSet(stage, scarf, tailwind, paralysis);
    }
}
