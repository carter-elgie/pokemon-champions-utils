using System.Text;
using System.Text.RegularExpressions;

namespace PokemonChampions.Shared.Extensions;

public static class StringExtensions
{
    private static readonly Regex NonAlphanumericRegex = new(@"[^a-z0-9]", RegexOptions.Compiled);

    /// <summary>
    /// Converts a display name or user input to the normalized form used for fast DB lookups.
    /// Lowercases and strips all spaces, hyphens, and other non-alphanumeric characters.
    /// Example: "Mega Charizard Y" → "megacharizardy", "charizard-mega-y" → "charizardmegay"
    /// </summary>
    public static string ToNormalizedId(this string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var lower = input.ToLowerInvariant();
        return NonAlphanumericRegex.Replace(lower, string.Empty);
    }

    /// <summary>
    /// Converts a display name to a Showdown-style ID (lowercase, hyphens instead of spaces,
    /// special characters stripped). Example: "Charizard Mega X" → "charizard-mega-x"
    /// </summary>
    public static string ToShowdownId(this string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var lower = input.ToLowerInvariant().Trim();
        var withHyphens = Regex.Replace(lower, @"\s+", "-");
        return Regex.Replace(withHyphens, @"[^a-z0-9\-]", string.Empty);
    }

    /// <summary>
    /// Computes the Levenshtein edit distance between two strings.
    /// Used for fuzzy name matching when exact lookup fails.
    /// </summary>
    public static int LevenshteinDistance(this string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= target.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    /// <summary>
    /// Capitalizes only the first character of a string, leaving the rest unchanged.
    /// </summary>
    public static string CapitalizeFirst(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input[1..];
    }
}
