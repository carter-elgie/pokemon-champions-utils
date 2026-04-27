using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

[Table("Item")]
public class ItemEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ShowdownId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string NormalizedId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? ShortDesc { get; set; }

    [MaxLength(1000)]
    public string? Desc { get; set; }

    public bool IsMegaStone { get; set; }

    /// <summary>Showdown ID of the base Pokemon that this Mega Stone evolves.</summary>
    [MaxLength(100)]
    public string? MegaStoneFor { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<UsageItemEntity> UsageEntries { get; set; } = [];
}
