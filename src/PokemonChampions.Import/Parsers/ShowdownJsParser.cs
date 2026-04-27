using System.Text;
using System.Text.Json;

namespace PokemonChampions.Import.Parsers;

/// <summary>
/// Parses Pokemon Showdown's JavaScript data files into JSON documents.
/// Showdown .js files use JavaScript object literal syntax — unquoted property names,
/// single-quoted strings, and JS comments — which is NOT valid JSON.
/// This parser strips the module wrapper and converts the content to valid JSON before parsing.
/// </summary>
public static class ShowdownJsParser
{
    /// <summary>
    /// Strips the JS module wrapper from a Showdown .js data file, converts the inner
    /// JavaScript object literal to valid JSON, and returns the parsed document.
    /// </summary>
    /// <param name="rawContent">Raw content of the .js file.</param>
    /// <param name="exportName">
    /// The JS export variable name, e.g. "BattleMovedex". Pass null to auto-detect.
    /// </param>
    public static JsonDocument Parse(string rawContent, string? exportName = null)
    {
        var content = rawContent.AsSpan().TrimStart();

        string jsContent;
        if (exportName != null)
        {
            var prefix = $"exports.{exportName} = ";
            if (!content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new FormatException(
                    $"Expected file to start with '{prefix}'. Got: {content[..Math.Min(80, content.Length)]}...");
            jsContent = content[prefix.Length..].TrimEnd().TrimEnd(';').ToString();
        }
        else
        {
            var eqIdx = content.IndexOf('=');
            if (eqIdx < 0)
                throw new FormatException("Could not find '=' in Showdown data file.");
            jsContent = content[(eqIdx + 1)..].TrimStart().TrimEnd().TrimEnd(';').ToString();
        }

        // Showdown .js files use JS object literal syntax, not JSON.
        // Convert to valid JSON before parsing.
        var json = JsObjectToJson(jsContent);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Parses a pure .json file (e.g. pokedex.json, learnsets.json — these are valid JSON).
    /// </summary>
    public static JsonDocument ParseJson(string rawContent) =>
        JsonDocument.Parse(rawContent);

    // ── JS → JSON converter ────────────────────────────────────────────────────

    /// <summary>
    /// Converts a JavaScript object literal string to valid JSON by:
    /// - Quoting unquoted property names
    /// - Converting single-quoted strings to double-quoted
    /// - Stripping // and /* */ comments
    /// - Replacing JS-only values (undefined → null)
    /// </summary>
    private static string JsObjectToJson(string js)
    {
        var sb = new StringBuilder(js.Length + js.Length / 4);
        int i = 0;
        int len = js.Length;

        while (i < len)
        {
            char c = js[i];

            // ── Double-quoted string — copy verbatim, honouring escapes ──────
            if (c == '"')
            {
                sb.Append('"');
                i++;
                while (i < len)
                {
                    char sc = js[i++];
                    sb.Append(sc);
                    if (sc == '\\' && i < len)
                        sb.Append(js[i++]);
                    else if (sc == '"')
                        break;
                }
                continue;
            }

            // ── Single-quoted string — convert to double-quoted ───────────────
            if (c == '\'')
            {
                sb.Append('"');
                i++;
                while (i < len && js[i] != '\'')
                {
                    if (js[i] == '\\')
                    {
                        i++;
                        if (i < len)
                        {
                            if (js[i] == '\'') { sb.Append('\''); i++; }  // \' → '
                            else { sb.Append('\\'); sb.Append(js[i++]); }
                        }
                    }
                    else if (js[i] == '"')
                    {
                        sb.Append('\\'); sb.Append('"'); i++;
                    }
                    else
                    {
                        sb.Append(js[i++]);
                    }
                }
                if (i < len) i++; // skip closing '
                sb.Append('"');
                continue;
            }

            // ── Line comment // ───────────────────────────────────────────────
            if (c == '/' && i + 1 < len && js[i + 1] == '/')
            {
                while (i < len && js[i] != '\n') i++;
                continue;
            }

            // ── Block comment /* */ ───────────────────────────────────────────
            if (c == '/' && i + 1 < len && js[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < len && !(js[i] == '*' && js[i + 1] == '/')) i++;
                if (i + 1 < len) i += 2;
                continue;
            }

            // ── After { or , — look for an unquoted property name ────────────
            if (c == '{' || c == ',')
            {
                sb.Append(c);
                i++;

                // Pass through whitespace
                while (i < len && IsWs(js[i]))
                    sb.Append(js[i++]);

                // If the next character starts an identifier (not " ' } ]), it
                // could be an unquoted property name.
                if (i < len && js[i] != '"' && js[i] != '\'' &&
                    js[i] != '}' && js[i] != ']' && IsIdentStart(js[i]))
                {
                    int start = i;
                    while (i < len && IsIdentCont(js[i])) i++;
                    var ident = js[start..i];

                    // Peek past whitespace to see if ':' follows (confirming it's a key)
                    int peek = i;
                    while (peek < len && IsWs(js[peek])) peek++;

                    if (peek < len && js[peek] == ':')
                    {
                        // Unquoted key — quote it
                        sb.Append('"').Append(ident).Append('"');
                    }
                    else
                    {
                        // Unquoted value identifier (e.g. undefined, true, false)
                        sb.Append(ident == "undefined" ? "null" : ident);
                    }
                }
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    private static bool IsWs(char c) => c is ' ' or '\t' or '\r' or '\n';
    private static bool IsIdentStart(char c) => char.IsAsciiLetter(c) || c == '_' || c == '$';
    private static bool IsIdentCont(char c) => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '$';
}
