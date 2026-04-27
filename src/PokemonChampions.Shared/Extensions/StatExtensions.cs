using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Shared.Extensions;

public static class StatExtensions
{
    /// <summary>
    /// Attempts to parse a user-provided token (or two consecutive tokens joined with a space)
    /// as a StatName. Accepts common abbreviations and full names.
    /// </summary>
    public static bool TryParseStatName(this string input, out StatName stat)
    {
        stat = default;
        return input.ToLowerInvariant().Trim() switch
        {
            "hp" or "health" or "hitpoints" => Assign(out stat, StatName.Hp),
            "atk" or "att" or "attack" => Assign(out stat, StatName.Atk),
            "def" or "defense" or "defence" => Assign(out stat, StatName.Def),
            "spa" or "sp.a" or "spatk" or "sp.atk" or "specialattack" or "special attack" or "sp. atk" => Assign(out stat, StatName.SpA),
            "spd" or "sp.d" or "spdef" or "sp.def" or "specialdefense" or "specialdefence" or "special defense" or "special defence" or "sp. def" => Assign(out stat, StatName.SpD),
            "spe" or "speed" => Assign(out stat, StatName.Spe),
            _ => false
        };
    }

    // Overload to check a sequence of 1 or 2 tokens for multi-word stat names.
    public static bool TryParseStatFromTokens(string[] tokens, int startIndex, out StatName stat, out int tokensConsumed)
    {
        stat = default;
        tokensConsumed = 0;

        if (startIndex >= tokens.Length) return false;

        // Try two tokens first ("special attack", "special defense")
        if (startIndex + 1 < tokens.Length)
        {
            var twoWord = tokens[startIndex] + " " + tokens[startIndex + 1];
            if (twoWord.TryParseStatName(out stat))
            {
                tokensConsumed = 2;
                return true;
            }
        }

        // Then one token
        if (tokens[startIndex].TryParseStatName(out stat))
        {
            tokensConsumed = 1;
            return true;
        }

        return false;
    }

    private static bool Assign(out StatName stat, StatName value)
    {
        stat = value;
        return true;
    }

    public static string DisplayName(this StatName stat) => stat switch
    {
        StatName.Hp  => "HP",
        StatName.Atk => "Attack",
        StatName.Def => "Defense",
        StatName.SpA => "Sp. Atk",
        StatName.SpD => "Sp. Def",
        StatName.Spe => "Speed",
        _            => stat.ToString()
    };

    public static string Abbreviation(this StatName stat) => stat switch
    {
        StatName.Hp  => "HP",
        StatName.Atk => "Atk",
        StatName.Def => "Def",
        StatName.SpA => "SpA",
        StatName.SpD => "SpD",
        StatName.Spe => "Spe",
        _            => stat.ToString()
    };
}
