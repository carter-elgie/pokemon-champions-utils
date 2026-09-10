using System.Text.RegularExpressions;
using PokemonChampions.Import.Dto;

namespace PokemonChampions.Import.Parsers;

/// <summary>
/// Parses pokepaste format text into a list of <see cref="ParsedTeamMember"/> records.
/// Pokepaste format: https://pokepast.es/
/// </summary>
public static class PokepasteParser
{
    // Stat abbreviation aliases used in pokepaste EVs/IVs lines
    private static readonly Dictionary<string, string> StatAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HP"] = "hp", ["Atk"] = "atk", ["Def"] = "def",
        ["SpA"] = "spa", ["SpD"] = "spd", ["Spe"] = "spe",
        ["Sp. Atk"] = "spa", ["Sp. Def"] = "spd",
        ["Special Attack"] = "spa", ["Special Defense"] = "spd", ["Speed"] = "spe",
        ["Attack"] = "atk", ["Defense"] = "def",
    };

    private static readonly Regex EvLine = new(@"(\d+)\s+([A-Za-z.]+(?:\s+[A-Za-z.]+)?)", RegexOptions.Compiled);
    private static readonly Regex NatureLine = new(@"^([A-Za-z]+)\s+Nature$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex SpeciesItemLine = new(@"^(?:(.+?)\s+\((.+?)\)|(.+?))\s*(?:@\s*(.+))?$", RegexOptions.Compiled);

    /// <summary>
    /// Parses a full pokepaste string into up to 6 team member records.
    /// Blank lines (or two consecutive newlines) separate team members.
    /// </summary>
    public static IReadOnlyList<ParsedTeamMember> Parse(string pokepaste)
    {
        var members = new List<ParsedTeamMember>();
        var blocks = SplitIntoBlocks(pokepaste);

        foreach (var block in blocks.Take(6))
        {
            var member = ParseBlock(block);
            if (member.Species != null)
                members.Add(member);
        }

        return members;
    }

    private static IEnumerable<string[]> SplitIntoBlocks(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var currentBlock = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentBlock.Count > 0)
                {
                    yield return [.. currentBlock];
                    currentBlock.Clear();
                }
            }
            else
            {
                currentBlock.Add(line.TrimEnd());
            }
        }

        if (currentBlock.Count > 0)
            yield return [.. currentBlock];
    }

    private static ParsedTeamMember ParseBlock(string[] lines)
    {
        var member = new ParsedTeamMember();
        var moveCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (i == 0)
            {
                ParseFirstLine(line, member);
                continue;
            }

            if (line.StartsWith("- "))
            {
                var moveName = line[2..].Trim();
                switch (++moveCount)
                {
                    case 1: member.Move1 = moveName; break;
                    case 2: member.Move2 = moveName; break;
                    case 3: member.Move3 = moveName; break;
                    case 4: member.Move4 = moveName; break;
                }
                continue;
            }

            if (line.StartsWith("Ability: ", StringComparison.OrdinalIgnoreCase))
            {
                member.Ability = line[9..].Trim();
                continue;
            }

            if (line.StartsWith("EVs: ", StringComparison.OrdinalIgnoreCase))
            {
                ParseStatLine(line[5..], member, isEvs: true);
                continue;
            }

            if (line.StartsWith("IVs: ", StringComparison.OrdinalIgnoreCase))
            {
                ParseStatLine(line[5..], member, isEvs: false);
                continue;
            }

            if (line.StartsWith("Level: ", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(line[7..].Trim(), out int lvl))
                    member.Level = lvl;
                continue;
            }

            // Nature line: "Timid Nature"
            var natureMatch = NatureLine.Match(line);
            if (natureMatch.Success)
            {
                member.Nature = natureMatch.Groups[1].Value;
                continue;
            }
        }

        return member;
    }

    private static void ParseFirstLine(string line, ParsedTeamMember member)
    {
        // Formats:
        //   "Incineroar @ Sitrus Berry"          — no nickname
        //   "Spud (Incineroar) @ Sitrus Berry"  — nickname (species)
        //   "Incineroar"                         — bare species, no item
        //   "Spud (Incineroar)"                  — nickname, no item

        string rest = line;
        string? item = null;

        var atIdx = line.LastIndexOf(" @ ");
        if (atIdx >= 0)
        {
            item = line[(atIdx + 3)..].Trim();
            rest = line[..atIdx].Trim();
        }

        member.Item = item;

        // Check for nickname (species) pattern
        var parenOpen = rest.IndexOf('(');
        var parenClose = rest.LastIndexOf(')');
        if (parenOpen >= 0 && parenClose > parenOpen)
        {
            member.Nickname = rest[..parenOpen].Trim();
            member.Species = rest[(parenOpen + 1)..parenClose].Trim();
        }
        else
        {
            member.Species = rest.Trim();
        }
    }

    private static void ParseStatLine(string statsPart, ParsedTeamMember member, bool isEvs)
    {
        // Format: "252 HP / 4 Atk / 252 SpD" or similar
        var entries = statsPart.Split('/');
        foreach (var entry in entries)
        {
            var trimmed = entry.Trim();
            var spaceIdx = trimmed.IndexOf(' ');
            if (spaceIdx < 0) continue;

            if (!int.TryParse(trimmed[..spaceIdx], out int value)) continue;
            var statName = trimmed[(spaceIdx + 1)..].Trim();

            if (!StatAliases.TryGetValue(statName, out var canonical)) continue;

            if (isEvs)
            {
                // Pokepaste EVs are stored in standard Gen 9 EV units (0–252).
                // Pokemon Champions uses stat points (1 stat point = 4 EVs),
                // so we convert: statPoints = floor(evs / 4)
                int statPoints = value / 4;
                switch (canonical)
                {
                    case "hp":  member.SpHp  = statPoints; break;
                    case "atk": member.SpAtk = statPoints; break;
                    case "def": member.SpDef = statPoints; break;
                    case "spa": member.SpSpa = statPoints; break;
                    case "spd": member.SpSpd = statPoints; break;
                    case "spe": member.SpSpe = statPoints; break;
                }
            }
            else
            {
                switch (canonical)
                {
                    case "hp":  member.IvHp  = value; break;
                    case "atk": member.IvAtk = value; break;
                    case "def": member.IvDef = value; break;
                    case "spa": member.IvSpa = value; break;
                    case "spd": member.IvSpd = value; break;
                    case "spe": member.IvSpe = value; break;
                }
            }
        }
    }
}
