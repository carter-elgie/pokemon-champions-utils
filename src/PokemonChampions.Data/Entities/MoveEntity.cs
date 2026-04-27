using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

[Table("Move")]
public class MoveEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ShowdownId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string NormalizedId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Type { get; set; }

    /// <summary>Physical, Special, or Status.</summary>
    [MaxLength(20)]
    public string? Category { get; set; }

    public int? Power { get; set; }

    /// <summary>Accuracy 1–100, or null for moves that never miss.</summary>
    public int? Accuracy { get; set; }

    public int Pp { get; set; }
    public int Priority { get; set; }

    [MaxLength(50)]
    public string? Target { get; set; }

    /// <summary>JSON blob of move flags, e.g. {"contact":1,"protect":1}.</summary>
    [MaxLength(500)]
    public string? Flags { get; set; }

    [MaxLength(300)]
    public string? ShortDesc { get; set; }

    [MaxLength(1000)]
    public string? Desc { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<LearnsetEntity> Learnsets { get; set; } = [];
    public ICollection<UsageMoveEntity> UsageEntries { get; set; } = [];
}
