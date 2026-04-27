using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

[Table("Team")]
public class TeamEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FormatShowdownId { get; set; }

    /// <summary>The raw pokepaste text — source of truth for the team.</summary>
    [Required]
    public string Pokepaste { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TeamMemberEntity> Members { get; set; } = [];
}

[Table("TeamMember")]
public class TeamMemberEntity
{
    [Key]
    public int Id { get; set; }

    public int TeamId { get; set; }
    [ForeignKey(nameof(TeamId))]
    public TeamEntity Team { get; set; } = null!;

    [MaxLength(100)]
    public string? PokemonShowdownId { get; set; }

    [MaxLength(100)]
    public string? Nickname { get; set; }

    [MaxLength(100)]
    public string? Item { get; set; }

    [MaxLength(100)]
    public string? Ability { get; set; }

    [MaxLength(20)]
    public string? Nature { get; set; }

    // Stat points (Pokemon Champions units, capped at 32 per stat, 66 total)
    public int SpHp { get; set; }
    public int SpAtk { get; set; }
    public int SpDef { get; set; }
    public int SpSpa { get; set; }
    public int SpSpd { get; set; }
    public int SpSpe { get; set; }

    // IVs (0–31)
    public int IvHp { get; set; } = 31;
    public int IvAtk { get; set; } = 31;
    public int IvDef { get; set; } = 31;
    public int IvSpa { get; set; } = 31;
    public int IvSpd { get; set; } = 31;
    public int IvSpe { get; set; } = 31;

    [MaxLength(100)]
    public string? Move1 { get; set; }

    [MaxLength(100)]
    public string? Move2 { get; set; }

    [MaxLength(100)]
    public string? Move3 { get; set; }

    [MaxLength(100)]
    public string? Move4 { get; set; }

    public int SlotIndex { get; set; }

    /// <summary>Null = not yet validated; true = legal; false = has violations.</summary>
    public bool? IsLegal { get; set; }

    [MaxLength(1000)]
    public string? LegalityNotes { get; set; }
}
