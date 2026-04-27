using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

[Table("Alias")]
public class AliasEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string AliasText { get; set; } = string.Empty;

    /// <summary>Entity type: "pokemon", "move", "item", or "ability".</summary>
    [Required, MaxLength(20)]
    public string TargetType { get; set; } = string.Empty;

    public int TargetId { get; set; }

    public DateTime CreatedAt { get; set; }
}

[Table("AppSetting")]
public class AppSettingEntity
{
    [Key, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}
