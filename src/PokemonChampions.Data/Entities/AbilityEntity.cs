using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

[Table("Ability")]
public class AbilityEntity
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

    public DateTime UpdatedAt { get; set; }

    public ICollection<UsageAbilityEntity> UsageEntries { get; set; } = [];
}
