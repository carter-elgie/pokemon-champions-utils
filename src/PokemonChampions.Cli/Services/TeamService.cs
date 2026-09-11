using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Legality;
using PokemonChampions.Core.Services;
using PokemonChampions.Data;
using PokemonChampions.Data.Entities;
using PokemonChampions.Import.Dto;
using PokemonChampions.Import.Parsers;
using PokemonChampions.Shared.Constants;
using PokemonChampions.Shared.Extensions;

namespace PokemonChampions.Cli.Services;

public class TeamService(AppDbContext db, ISettingsService settings, FormatRegistry formatRegistry) : ITeamService
{
    public async Task<IReadOnlyList<Team>> GetAllAsync(string? formatShowdownId = null, CancellationToken ct = default)
    {
        var query = db.Teams.Include(t => t.Members).AsQueryable();
        if (formatShowdownId is not null)
            query = query.Where(t => t.FormatShowdownId == formatShowdownId);
        var entities = await query.OrderBy(t => t.Name).ToListAsync(ct);
        return entities.Select(MapToDomain).ToList();
    }

    public async Task<Team?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var entity = await db.Teams.Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Name == name, ct);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Team?> GetActiveAsync(CancellationToken ct = default)
    {
        var name = await settings.GetAsync(AppConstants.SettingKeys.ActiveTeamName, ct);
        return name is null ? null : await GetByNameAsync(name, ct);
    }

    public async Task<Team> ImportPokepasteAsync(
        string name,
        string pokepaste,
        string? formatShowdownId = null,
        CancellationToken ct = default)
    {
        var existing = await db.Teams.FirstOrDefaultAsync(t => t.Name == name, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A team named '{name}' already exists. Use 'teams edit' to update it.");

        return await SaveTeamAsync(name, pokepaste, formatShowdownId, existing: null, ct);
    }

    public async Task<Team> UpdatePokepasteAsync(
        string name,
        string newPokepaste,
        CancellationToken ct = default)
    {
        var existing = await db.Teams.Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Name == name, ct);
        if (existing is null)
            throw new InvalidOperationException($"Team '{name}' not found.");

        return await SaveTeamAsync(name, newPokepaste, existing.FormatShowdownId, existing, ct);
    }

    public async Task SetActiveAsync(string name, CancellationToken ct = default)
    {
        var exists = await db.Teams.AnyAsync(t => t.Name == name, ct);
        if (!exists)
            throw new InvalidOperationException($"Team '{name}' not found.");
        await settings.SetAsync(AppConstants.SettingKeys.ActiveTeamName, name, ct);
    }

    public async Task DeleteAsync(string name, CancellationToken ct = default)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Name == name, ct);
        if (team is null)
            throw new InvalidOperationException($"Team '{name}' not found.");
        db.Teams.Remove(team);

        var activeName = await settings.GetAsync(AppConstants.SettingKeys.ActiveTeamName, ct);
        if (string.Equals(activeName, name, StringComparison.OrdinalIgnoreCase))
            await settings.SetAsync(AppConstants.SettingKeys.ActiveTeamName, string.Empty, ct);

        await db.SaveChangesAsync(ct);
    }

    public async Task<LegalityReport> ValidateAsync(int teamId, string formatShowdownId, CancellationToken ct = default)
    {
        var entity = await db.Teams.Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct)
            ?? throw new InvalidOperationException($"Team ID {teamId} not found.");

        var format = formatRegistry.TryGet(formatShowdownId)
            ?? throw new InvalidOperationException($"Format '{formatShowdownId}' is not registered.");

        var team = MapToDomain(entity);
        var report = LegalityChecker.Check(team, format);

        foreach (var memberEntity in entity.Members)
        {
            var memberViolations = report.Violations
                .Where(v => v.SlotIndex == memberEntity.SlotIndex)
                .Select(v => v.Message)
                .ToList();

            memberEntity.IsLegal     = memberViolations.Count == 0;
            memberEntity.LegalityNotes = memberViolations.Count > 0
                ? string.Join("; ", memberViolations)
                : null;
        }

        await db.SaveChangesAsync(ct);
        return report;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Team> SaveTeamAsync(
        string name,
        string pokepaste,
        string? formatShowdownId,
        TeamEntity? existing,
        CancellationToken ct)
    {
        var parsed = PokepasteParser.Parse(pokepaste);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            var newTeam = new TeamEntity
            {
                Name             = name,
                FormatShowdownId = formatShowdownId,
                Pokepaste        = pokepaste,
                CreatedAt        = now,
                UpdatedAt        = now
            };
            await BuildMembersAsync(newTeam, parsed, ct);
            db.Teams.Add(newTeam);
            await db.SaveChangesAsync(ct);
            return MapToDomain(newTeam);
        }
        else
        {
            db.RemoveRange(existing.Members);
            existing.Pokepaste = pokepaste;
            existing.UpdatedAt = now;
            existing.Members.Clear();
            await BuildMembersAsync(existing, parsed, ct);
            await db.SaveChangesAsync(ct);
            return MapToDomain(existing);
        }
    }

    private async Task BuildMembersAsync(TeamEntity team, IReadOnlyList<ParsedTeamMember> parsed, CancellationToken ct)
    {
        int slot = 0;
        foreach (var p in parsed.Take(6))
        {
            team.Members.Add(new TeamMemberEntity
            {
                PokemonShowdownId = await ResolveShowdownIdAsync(p.Species, p.Gender, ct),
                Nickname          = string.IsNullOrWhiteSpace(p.Nickname) ? null : p.Nickname,
                Item              = p.Item,
                Ability           = p.Ability,
                Nature            = p.Nature,
                SpHp  = p.SpHp,
                SpAtk = p.SpAtk,
                SpDef = p.SpDef,
                SpSpa = p.SpSpa,
                SpSpd = p.SpSpd,
                SpSpe = p.SpSpe,
                IvHp  = p.IvHp,
                IvAtk = p.IvAtk,
                IvDef = p.IvDef,
                IvSpa = p.IvSpa,
                IvSpd = p.IvSpd,
                IvSpe = p.IvSpe,
                Move1     = p.Move1,
                Move2     = p.Move2,
                Move3     = p.Move3,
                Move4     = p.Move4,
                SlotIndex = slot++
            });
        }
    }

    // Some species have a distinct Female form with different stats/ability/movepool
    // (e.g. Basculegion-F, Indeedee-F). Male is always the default/base DB entry, so
    // only a Female marker needs to try resolving to a separate "<species>f" form.
    private async Task<string?> ResolveShowdownIdAsync(string? species, string? gender, CancellationToken ct)
    {
        if (species is null) return null;

        var baseId = species.ToNormalizedId();
        if (string.Equals(gender, "F", StringComparison.OrdinalIgnoreCase))
        {
            var femaleId = baseId + "f";
            if (await db.Pokemon.AnyAsync(p => p.ShowdownId == femaleId, ct))
                return femaleId;
        }

        return baseId;
    }

    internal static Team MapToDomain(TeamEntity e) => new()
    {
        Id               = e.Id,
        Name             = e.Name,
        FormatShowdownId = e.FormatShowdownId,
        Pokepaste        = e.Pokepaste,
        CreatedAt        = e.CreatedAt,
        UpdatedAt        = e.UpdatedAt,
        Members          = e.Members
            .OrderBy(m => m.SlotIndex)
            .Select(MapMemberToDomain)
            .ToList()
    };

    private static TeamMember MapMemberToDomain(TeamMemberEntity e) => new()
    {
        Id                = e.Id,
        TeamId            = e.TeamId,
        PokemonShowdownId = e.PokemonShowdownId,
        Nickname          = e.Nickname,
        Item              = e.Item,
        Ability           = e.Ability,
        Nature            = e.Nature,
        StatPoints        = new EvSpread(e.SpHp, e.SpAtk, e.SpDef, e.SpSpa, e.SpSpd, e.SpSpe),
        Ivs               = new IvSpread(e.IvHp, e.IvAtk, e.IvDef, e.IvSpa, e.IvSpd, e.IvSpe),
        Move1             = e.Move1,
        Move2             = e.Move2,
        Move3             = e.Move3,
        Move4             = e.Move4,
        SlotIndex         = e.SlotIndex,
        IsLegal           = e.IsLegal,
        LegalityNotes     = e.LegalityNotes
    };
}
