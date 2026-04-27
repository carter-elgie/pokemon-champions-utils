using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PokemonChampions.Data.Entities;

/// <summary>One row per (pokemon, move, generation, learn method) combination.</summary>
[Table("Learnset")]
public class LearnsetEntity
{
    [Key]
    public int Id { get; set; }

    public int PokemonId { get; set; }
    [ForeignKey(nameof(PokemonId))]
    public PokemonEntity Pokemon { get; set; } = null!;

    public int MoveId { get; set; }
    [ForeignKey(nameof(MoveId))]
    public MoveEntity Move { get; set; } = null!;

    public int Generation { get; set; }

    /// <summary>Learn method: L=levelup, M=machine, E=egg, T=tutor, S=event.</summary>
    [MaxLength(2)]
    public string Method { get; set; } = string.Empty;
}
