namespace PokemonChampions.Core.Legality;

/// <summary>Result of a full team legality check.</summary>
public class LegalityReport
{
    public bool IsLegal => !Violations.Any();
    public IReadOnlyList<LegalityViolation> Violations { get; init; } = [];
    public IReadOnlyList<LegalityWarning> Warnings { get; init; } = [];
}

/// <summary>A hard legality violation that makes a team illegal.</summary>
public record LegalityViolation(string Message, int? SlotIndex = null);

/// <summary>
/// A non-blocking warning about a potentially incorrect but legal build
/// (e.g., using a type-resist berry for a type the holder isn't weak to).
/// </summary>
public record LegalityWarning(string Message, int? SlotIndex = null);
