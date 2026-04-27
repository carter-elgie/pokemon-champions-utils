using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Core.Domain;

/// <summary>
/// Represents a Pokemon nature and its stat modifiers.
/// Boosted stat is ×1.1, hindered stat is ×0.9, neutral stats are ×1.0.
/// </summary>
public record Nature(string Name, StatName? BoostedStat, StatName? HinderedStat)
{
    public double GetMultiplier(StatName stat)
    {
        if (stat == StatName.Hp) return 1.0;
        if (stat == BoostedStat) return 1.1;
        if (stat == HinderedStat) return 0.9;
        return 1.0;
    }

    public bool IsNeutral => BoostedStat == null || BoostedStat == HinderedStat;

    public static readonly IReadOnlyDictionary<string, Nature> All = BuildAll();

    public static Nature? TryGet(string name) =>
        All.TryGetValue(name.ToLowerInvariant(), out var n) ? n : null;

    private static Dictionary<string, Nature> BuildAll() => new()
    {
        ["hardy"]   = new("Hardy",   null,         null),
        ["lonely"]  = new("Lonely",  StatName.Atk, StatName.Def),
        ["brave"]   = new("Brave",   StatName.Atk, StatName.Spe),
        ["adamant"] = new("Adamant", StatName.Atk, StatName.SpA),
        ["naughty"] = new("Naughty", StatName.Atk, StatName.SpD),
        ["bold"]    = new("Bold",    StatName.Def, StatName.Atk),
        ["docile"]  = new("Docile",  null,         null),
        ["relaxed"] = new("Relaxed", StatName.Def, StatName.Spe),
        ["impish"]  = new("Impish",  StatName.Def, StatName.SpA),
        ["lax"]     = new("Lax",     StatName.Def, StatName.SpD),
        ["timid"]   = new("Timid",   StatName.Spe, StatName.Atk),
        ["hasty"]   = new("Hasty",   StatName.Spe, StatName.Def),
        ["serious"] = new("Serious", null,         null),
        ["jolly"]   = new("Jolly",   StatName.Spe, StatName.SpA),
        ["naive"]   = new("Naive",   StatName.Spe, StatName.SpD),
        ["modest"]  = new("Modest",  StatName.SpA, StatName.Atk),
        ["mild"]    = new("Mild",    StatName.SpA, StatName.Def),
        ["quiet"]   = new("Quiet",   StatName.SpA, StatName.Spe),
        ["bashful"] = new("Bashful", null,         null),
        ["rash"]    = new("Rash",    StatName.SpA, StatName.SpD),
        ["calm"]    = new("Calm",    StatName.SpD, StatName.Atk),
        ["gentle"]  = new("Gentle",  StatName.SpD, StatName.Def),
        ["sassy"]   = new("Sassy",   StatName.SpD, StatName.Spe),
        ["careful"] = new("Careful", StatName.SpD, StatName.SpA),
        ["quirky"]  = new("Quirky",  null,         null),
    };
}
