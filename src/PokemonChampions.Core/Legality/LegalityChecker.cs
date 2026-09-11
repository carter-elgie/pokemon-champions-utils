using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Formats;
using PokemonChampions.Shared.Constants;

namespace PokemonChampions.Core.Legality;

/// <summary>
/// Validates a team against a format definition.
/// Pure logic — no I/O or database access.
/// </summary>
public static class LegalityChecker
{
    public static LegalityReport Check(Team team, IFormatDefinition format)
    {
        var violations = new List<LegalityViolation>();
        var warnings   = new List<LegalityWarning>();

        foreach (var member in team.Members)
        {
            CheckMember(member, format, violations, warnings);
        }

        // Team-level constraints (Species Clause, Item Clause, etc.)
        foreach (var constraint in format.TeamConstraints)
        {
            var msgs = constraint.Validate(team.Members);
            violations.AddRange(msgs.Select(m => new LegalityViolation(m)));
        }

        // Team size
        if (team.Members.Count > format.TeamPreviewSize)
            violations.Add(new LegalityViolation(
                $"Team has {team.Members.Count} members; max is {format.TeamPreviewSize}."));
        else if (team.Members.Count < format.TeamPreviewSize)
            warnings.Add(new LegalityWarning(
                $"Team has {team.Members.Count} member(s); expected {format.TeamPreviewSize}."));

        return new LegalityReport { Violations = violations, Warnings = warnings };
    }

    private static void CheckMember(
        TeamMember member,
        IFormatDefinition format,
        List<LegalityViolation> violations,
        List<LegalityWarning> warnings)
    {
        var slot = member.SlotIndex;
        var label = member.PokemonShowdownId ?? $"Slot {slot + 1}";

        // Species
        if (member.PokemonShowdownId is not null &&
            !format.IsPokemonAllowed(member.PokemonShowdownId))
        {
            violations.Add(new LegalityViolation(
                $"{label} is not legal in {format.DisplayName}.", slot));
        }

        // Item
        if (member.Item is not null && !format.IsItemAllowed(member.Item.ToLowerInvariant()))
            violations.Add(new LegalityViolation($"{label}: item '{member.Item}' is banned.", slot));

        // Moves
        foreach (var move in member.GetMoves())
        {
            if (!format.IsMoveAllowed(move.ToLowerInvariant()))
                violations.Add(new LegalityViolation($"{label}: move '{move}' is banned.", slot));
        }

        // Ability
        if (member.Ability is not null && !format.IsAbilityAllowed(member.Ability.ToLowerInvariant()))
            violations.Add(new LegalityViolation($"{label}: ability '{member.Ability}' is banned.", slot));

        // Stat points — per-stat cap
        var sp = member.StatPoints;
        void CheckStat(int val, string name)
        {
            if (val > AppConstants.MaxStatPointsPerStat)
                violations.Add(new LegalityViolation(
                    $"{label}: {name} stat points ({val}) exceed the max of {AppConstants.MaxStatPointsPerStat}.", slot));
            if (val < 0)
                violations.Add(new LegalityViolation(
                    $"{label}: {name} stat points cannot be negative.", slot));
        }
        CheckStat(sp.Hp,  "HP");
        CheckStat(sp.Atk, "Attack");
        CheckStat(sp.Def, "Defense");
        CheckStat(sp.SpA, "Sp. Atk");
        CheckStat(sp.SpD, "Sp. Def");
        CheckStat(sp.Spe, "Speed");

        // Stat points — total cap
        if (sp.Total > AppConstants.MaxTotalStatPoints)
            violations.Add(new LegalityViolation(
                $"{label}: total stat points ({sp.Total}) exceed the max of {AppConstants.MaxTotalStatPoints}.", slot));

        // IVs (0–31)
        var ivs = member.Ivs;
        void CheckIv(int val, string name)
        {
            if (val < 0 || val > 31)
                violations.Add(new LegalityViolation(
                    $"{label}: {name} IV ({val}) must be 0–31.", slot));
        }
        CheckIv(ivs.Hp,  "HP");
        CheckIv(ivs.Atk, "Attack");
        CheckIv(ivs.Def, "Defense");
        CheckIv(ivs.SpA, "Sp. Atk");
        CheckIv(ivs.SpD, "Sp. Def");
        CheckIv(ivs.Spe, "Speed");

        // Warn about missing data that could indicate an import issue
        if (member.PokemonShowdownId is null)
            warnings.Add(new LegalityWarning($"Slot {slot + 1}: species could not be identified.", slot));
    }
}
