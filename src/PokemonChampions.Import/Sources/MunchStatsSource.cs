using System.Globalization;
using System.Text.RegularExpressions;
using PokemonChampions.Shared.Constants;

namespace PokemonChampions.Import.Sources;

/// <summary>
/// Fetches and parses usage statistics from MunchStats (munchstats.com).
/// MunchStats serves server-rendered HTML; data is extracted from the embedded
/// left-text / right-text span pairs within each named section.
/// </summary>
public class MunchStatsSource(HttpClient http)
{
    // Pair: left-text span (name) followed within ~400 chars by right-text span (number%)
    private static readonly Regex PairPattern = new(
        "class=\"left-text[^\"]*\">([^<]+)</span>[\\s\\S]{0,400}?class=\"right-text\">([0-9]+\\.?[0-9]*)%</span>",
        RegexOptions.Compiled);

    private static readonly Regex OverallUsagePattern = new(
        "Usage: <span[^>]*>([0-9]+\\.?[0-9]*)%</span>",
        RegexOptions.Compiled);

    private static readonly Regex MonthPattern = new(
        "Usage stats data from the month of (\\w+ \\d{4})",
        RegexOptions.Compiled);

    private static readonly Regex SectionSplitPattern = new(
        "<h[23][^>]*>",
        RegexOptions.Compiled);

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the full Pokemon usage list for a format from a single MunchStats page.
    /// Every page's sidebar contains all Pokemon with their usage percentages.
    /// Uses Incineroar as the anchor Pokemon since it's reliably present.
    /// </summary>
    public async Task<FormatUsageResult> GetFormatUsageListAsync(string munchStatsFormatId, CancellationToken ct)
    {
        var url = $"{AppConstants.MunchStats.Base}/{munchStatsFormatId}/0/Incineroar";
        var html = await http.GetStringAsync(url, ct);
        return ParseFormatUsage(html);
    }

    /// <summary>
    /// Fetches top move usage for a specific Pokemon.
    /// Returns null if the Pokemon is not found on MunchStats for this format.
    /// </summary>
    public async Task<PokemonMoveResult?> GetPokemonMovesAsync(
        string munchStatsFormatId, string pokemonDisplayName, CancellationToken ct)
    {
        var encodedName = Uri.EscapeDataString(pokemonDisplayName);
        var url = $"{AppConstants.MunchStats.Base}/{munchStatsFormatId}/0/{encodedName}";
        string html;
        try
        {
            html = await http.GetStringAsync(url, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        return ParsePokemonMoves(html);
    }

    // ── Parsing ────────────────────────────────────────────────────────────────

    private FormatUsageResult ParseFormatUsage(string html)
    {
        var month = ParseMonth(html);
        var sections = SplitBySections(html);
        var pokemon = new List<(string Name, double Pct)>();

        foreach (var (title, content) in sections)
        {
            if (!title.Contains("Monthly Rank", StringComparison.OrdinalIgnoreCase)) continue;
            pokemon.AddRange(ExtractPairs(content));
            break;
        }

        return new FormatUsageResult(pokemon, month);
    }

    private PokemonMoveResult ParsePokemonMoves(string html)
    {
        var month = ParseMonth(html);
        var sections = SplitBySections(html);
        var moves = new List<(string Name, double Pct)>();

        foreach (var (title, content) in sections)
        {
            if (!title.Trim().Equals("Moves", StringComparison.OrdinalIgnoreCase)) continue;
            moves.AddRange(ExtractPairs(content));
            break;
        }

        return new PokemonMoveResult(moves, month);
    }

    private string? ParseMonth(string html)
    {
        var m = MonthPattern.Match(html);
        if (!m.Success) return null;
        if (DateTime.TryParseExact(m.Groups[1].Value, "MMMM yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.ToString("yyyy-MM");
        return null;
    }

    private IEnumerable<(string Title, string Content)> SplitBySections(string html)
    {
        var parts = SectionSplitPattern.Split(html);
        foreach (var part in parts)
        {
            var closeIdx = part.IndexOf("</h", StringComparison.OrdinalIgnoreCase);
            if (closeIdx < 0) continue;
            yield return (part[..closeIdx], part[closeIdx..]);
        }
    }

    private List<(string Name, double Pct)> ExtractPairs(string content)
    {
        var results = new List<(string, double)>();
        foreach (Match m in PairPattern.Matches(content))
        {
            var name = m.Groups[1].Value.Trim();
            if (double.TryParse(m.Groups[2].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double pct))
                results.Add((name, pct));
        }
        return results;
    }
}

public record FormatUsageResult(
    IReadOnlyList<(string DisplayName, double UsagePct)> Pokemon,
    string? Month);

public record PokemonMoveResult(
    IReadOnlyList<(string MoveName, double UsagePct)> Moves,
    string? Month);
