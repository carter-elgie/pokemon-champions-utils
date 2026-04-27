using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

/// <summary>Aggregated usage statistics for a Pokemon in a specific format and data source.</summary>
[Table("UsageStats")]
public class UsageStatsEntity
{
    [Key]
    public int Id { get; set; }

    public int PokemonId { get; set; }
    [ForeignKey(nameof(PokemonId))]
    public PokemonEntity Pokemon { get; set; } = null!;

    [Required, MaxLength(100)]
    public string FormatShowdownId { get; set; } = string.Empty;

    public double UsagePct { get; set; }
    public int? RawCount { get; set; }

    /// <summary>"live" for MunchStats real-time data, or "YYYY-MM" for Smogon monthly data.</summary>
    [MaxLength(20)]
    public string? StatsMonth { get; set; }

    /// <summary>"munchstats" or "smogon-chaos".</summary>
    [Required, MaxLength(30)]
    public string Source { get; set; } = string.Empty;

    public DateTime FetchedAt { get; set; }

    public ICollection<UsageMoveEntity> Moves { get; set; } = [];
    public ICollection<UsageItemEntity> Items { get; set; } = [];
    public ICollection<UsageAbilityEntity> Abilities { get; set; } = [];
    public ICollection<UsageSpreadEntity> Spreads { get; set; } = [];
}

[Table("UsageMove")]
public class UsageMoveEntity
{
    [Key]
    public int Id { get; set; }

    public int StatsId { get; set; }
    [ForeignKey(nameof(StatsId))]
    public UsageStatsEntity Stats { get; set; } = null!;

    public int MoveId { get; set; }
    [ForeignKey(nameof(MoveId))]
    public MoveEntity Move { get; set; } = null!;

    public double UsagePct { get; set; }
    public int Rank { get; set; }
}

[Table("UsageItem")]
public class UsageItemEntity
{
    [Key]
    public int Id { get; set; }

    public int StatsId { get; set; }
    [ForeignKey(nameof(StatsId))]
    public UsageStatsEntity Stats { get; set; } = null!;

    public int ItemId { get; set; }
    [ForeignKey(nameof(ItemId))]
    public ItemEntity Item { get; set; } = null!;

    public double UsagePct { get; set; }
    public int Rank { get; set; }
}

[Table("UsageAbility")]
public class UsageAbilityEntity
{
    [Key]
    public int Id { get; set; }

    public int StatsId { get; set; }
    [ForeignKey(nameof(StatsId))]
    public UsageStatsEntity Stats { get; set; } = null!;

    public int AbilityId { get; set; }
    [ForeignKey(nameof(AbilityId))]
    public AbilityEntity Ability { get; set; } = null!;

    public double UsagePct { get; set; }
    public int Rank { get; set; }
}

/// <summary>An EV spread + nature combination and how frequently it appears in battle data.</summary>
[Table("UsageSpread")]
public class UsageSpreadEntity
{
    [Key]
    public int Id { get; set; }

    public int StatsId { get; set; }
    [ForeignKey(nameof(StatsId))]
    public UsageStatsEntity Stats { get; set; } = null!;

    [Required, MaxLength(20)]
    public string Nature { get; set; } = string.Empty;

    /// <summary>Stat points (Pokemon Champions units) for each stat.</summary>
    public int SpHp { get; set; }
    public int SpAtk { get; set; }
    public int SpDef { get; set; }
    public int SpSpa { get; set; }
    public int SpSpd { get; set; }
    public int SpSpe { get; set; }

    public double UsagePct { get; set; }
    public int Rank { get; set; }
}
