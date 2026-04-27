namespace PokemonChampions.Import.Dto;

/// <summary>Intermediate result from parsing a pokepaste block for a single Pokemon.</summary>
public class ParsedTeamMember
{
    public string? Species { get; set; }
    public string? Nickname { get; set; }
    public string? Item { get; set; }
    public string? Ability { get; set; }
    public string? Nature { get; set; }
    public int? Level { get; set; }

    /// <summary>Stat points (Pokemon Champions units) per stat. Defaults to 0.</summary>
    public int SpHp { get; set; }
    public int SpAtk { get; set; }
    public int SpDef { get; set; }
    public int SpSpa { get; set; }
    public int SpSpd { get; set; }
    public int SpSpe { get; set; }

    public int IvHp { get; set; } = 31;
    public int IvAtk { get; set; } = 31;
    public int IvDef { get; set; } = 31;
    public int IvSpa { get; set; } = 31;
    public int IvSpd { get; set; } = 31;
    public int IvSpe { get; set; } = 31;

    public string? Move1 { get; set; }
    public string? Move2 { get; set; }
    public string? Move3 { get; set; }
    public string? Move4 { get; set; }

    public List<string> ParseWarnings { get; } = [];
}
