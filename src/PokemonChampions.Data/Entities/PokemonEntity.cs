using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

/// <summary>Database entity for a Pokemon species or form.</summary>
[Table("Pokemon")]
public class PokemonEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ShowdownId { get; set; } = string.Empty;

    /// <summary>ShowdownId lowercased with all hyphens removed — used for fast case/hyphen-insensitive lookups.</summary>
    [Required, MaxLength(100)]
    public string NormalizedId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public int BaseHp { get; set; }
    public int BaseAtk { get; set; }
    public int BaseDef { get; set; }
    public int BaseSpa { get; set; }
    public int BaseSpd { get; set; }
    public int BaseSpe { get; set; }

    [MaxLength(20)]
    public string? Type1 { get; set; }

    [MaxLength(20)]
    public string? Type2 { get; set; }

    [MaxLength(100)]
    public string? Ability0 { get; set; }

    [MaxLength(100)]
    public string? Ability1 { get; set; }

    [MaxLength(100)]
    public string? AbilityH { get; set; }

    public bool IsMega { get; set; }

    [MaxLength(100)]
    public string? BaseFormShowdownId { get; set; }

    /// <summary>Comma-separated list of format IDs in which this Pokemon is banned.</summary>
    [MaxLength(500)]
    public string? FormatBans { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<LearnsetEntity> Learnsets { get; set; } = [];
    public ICollection<UsageStatsEntity> UsageStats { get; set; } = [];
}
