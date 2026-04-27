using PokemonChampions.Shared.Enums;
using Spectre.Console;

namespace PokemonChampions.Cli.Rendering;

/// <summary>Maps each Pokemon type to a Spectre.Console color for rich terminal display.</summary>
public static class TypeColors
{
    private static readonly Dictionary<PokemonType, Color> Map = new()
    {
        [PokemonType.Normal]   = new Color(168, 168, 120),
        [PokemonType.Fire]     = new Color(240, 128,  48),
        [PokemonType.Water]    = new Color(104, 144, 240),
        [PokemonType.Electric] = new Color(248, 208,  48),
        [PokemonType.Grass]    = new Color(120, 200,  80),
        [PokemonType.Ice]      = new Color(152, 216, 216),
        [PokemonType.Fighting] = new Color(192,  48,  40),
        [PokemonType.Poison]   = new Color(160,  64, 160),
        [PokemonType.Ground]   = new Color(224, 192,  80),
        [PokemonType.Flying]   = new Color(168, 144, 240),
        [PokemonType.Psychic]  = new Color(248,  88, 136),
        [PokemonType.Bug]      = new Color(168, 184,  32),
        [PokemonType.Rock]     = new Color(184, 160,  56),
        [PokemonType.Ghost]    = new Color(112,  88, 152),
        [PokemonType.Dragon]   = new Color(112,  56, 248),
        [PokemonType.Dark]     = new Color(112,  88,  72),
        [PokemonType.Steel]    = new Color(184, 184, 208),
        [PokemonType.Fairy]    = new Color(238, 153, 172),
        [PokemonType.Stellar]  = new Color(102, 204, 255),
        [PokemonType.None]     = Color.Grey,
    };

    public static Color Get(PokemonType type) =>
        Map.TryGetValue(type, out var color) ? color : Color.Grey;

    /// <summary>Returns Spectre markup for a type badge, e.g. "[bold #F08030] Fire [/]".</summary>
    public static string Badge(PokemonType type)
    {
        if (type == PokemonType.None) return string.Empty;
        var color = Get(type);
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        return $"[bold {hex}]{type}[/]";
    }

    /// <summary>Returns the type string with its color markup applied.</summary>
    public static string Colorize(PokemonType type, string? label = null)
    {
        if (type == PokemonType.None) return string.Empty;
        var color = Get(type);
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        return $"[{hex}]{label ?? type.ToString()}[/]";
    }
}
