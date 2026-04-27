using PokemonChampions.Core.Domain;
using PokemonChampions.Core.Services;
using PokemonChampions.Shared.Enums;

namespace PokemonChampions.Cli.Parsing;

/// <summary>
/// Resolves a sequence of user tokens to a game entity without requiring quotes or hyphens.
/// Resolution pipeline:
///   1. Join all candidate tokens and normalize (lowercase, strip non-alphanumeric).
///   2. Check each entity type for an exact NormalizedId match.
///   3. Check alias table.
///   4. Try common Pokemon name rewrites (mega X → X-mega, alolan X → X-alola, etc.).
///   5. Return all matches found (caller handles disambiguation when > 1 type matched).
/// </summary>
public class MultiWordNameParser(
    IPokemonService pokemonService,
    IMoveService moveService,
    IItemService itemService,
    IAbilityService abilityService,
    IAliasService aliasService)
{
    // Common prefix rewrites: normalized prefix → suffix to append to rest of name
    // Key: normalized prefix, Value: Showdown suffix
    private static readonly (string Prefix, string Suffix)[] PrefixRewrites =
    [
        ("mega",   "-mega"),
        ("alolan", "-alola"),
        ("galarian","galarian"),
        ("hisuian", "hisui"),
        ("paldean", "paldea"),
    ];

    public record LookupResult(EntityType Type, object Entity, int TokensConsumed);

    /// <summary>
    /// Tries to match as many tokens as possible (greedily) to any entity type.
    /// Returns all entity matches found for the best token count.
    /// The caller should show a disambiguation prompt when multiple types match.
    /// Supports an explicit type hint as the last token, e.g. "(item)" or "(move)",
    /// which filters results to that type and bypasses disambiguation.
    /// </summary>
    public async Task<IReadOnlyList<LookupResult>> ResolveAsync(
        string[] tokens,
        int startIndex,
        CancellationToken ct = default)
    {
        if (startIndex >= tokens.Length) return [];

        var slice = tokens[startIndex..];

        // If the last token is an explicit type hint like "(item)", resolve without it
        // and return only the matching type — no disambiguation menu.
        if (TryExtractTypeHint(slice, out var typeHint, out var nameTokens))
        {
            for (int len = nameTokens.Length; len >= 1; len--)
            {
                var span = nameTokens[..len];
                var joined = string.Join(" ", span);
                var all = await FindAllAsync(joined, ct);
                if (all.Count == 0) all = await TryPrefixRewriteAsync(span, ct);
                var typed = all.Where(m => m.Type == typeHint).ToList();
                if (typed.Count > 0)
                    return typed.Select(m => new LookupResult(m.Type, m.Entity, len + 1)).ToList();
            }
            // Hint matched nothing — fall through to normal resolution without the hint
        }

        // Normal resolution (greedy longest-match across all entity types)
        for (int len = slice.Length; len >= 1; len--)
        {
            var span = slice[..len];
            var joined = string.Join(" ", span);

            var matches = await FindAllAsync(joined, ct);

            if (matches.Count == 0)
                matches = await TryPrefixRewriteAsync(span, ct);

            if (matches.Count > 0)
                return matches.Select(m => new LookupResult(m.Type, m.Entity, len)).ToList();
        }

        return [];
    }

    private static bool TryExtractTypeHint(string[] tokens, out EntityType typeHint, out string[] nameTokens)
    {
        typeHint = default;
        nameTokens = tokens;
        if (tokens.Length < 2) return false;

        typeHint = tokens[^1].ToLowerInvariant() switch
        {
            "(pokemon)" => EntityType.Pokemon,
            "(move)"    => EntityType.Move,
            "(item)"    => EntityType.Item,
            "(ability)" => EntityType.Ability,
            _ => (EntityType)(-1)
        };

        if ((int)typeHint == -1) return false;

        nameTokens = tokens[..^1];
        return true;
    }

    /// <summary>
    /// Resolves a pre-joined name string (no token splitting here) to matching entities.
    /// </summary>
    public async Task<IReadOnlyList<(EntityType Type, object Entity)>> FindAllAsync(
        string input,
        CancellationToken ct = default)
    {
        // Check alias first — alias lookup is case-insensitive (DB NOCASE collation)
        var aliasResult = await aliasService.ResolveAsync(input, ct);
        if (aliasResult.HasValue)
        {
            var (aType, aId) = aliasResult.Value;
            var entity = await LookupByIdAsync(aType, aId, ct);
            if (entity is not null)
                return [(aType, entity)];
        }

        var results = new List<(EntityType, object)>();

        var pokemon = await pokemonService.FindAsync(input, ct);
        if (pokemon is not null) results.Add((EntityType.Pokemon, pokemon));

        var move = await moveService.FindAsync(input, ct);
        if (move is not null) results.Add((EntityType.Move, move));

        var item = await itemService.FindAsync(input, ct);
        if (item is not null) results.Add((EntityType.Item, item));

        var ability = await abilityService.FindAsync(input, ct);
        if (ability is not null) results.Add((EntityType.Ability, ability));

        return results;
    }

    private async Task<IReadOnlyList<(EntityType Type, object Entity)>> TryPrefixRewriteAsync(
        string[] tokens,
        CancellationToken ct)
    {
        foreach (var (prefix, suffix) in PrefixRewrites)
        {
            if (tokens.Length < 2) continue;
            var firstNorm = tokens[0].ToLowerInvariant();
            if (firstNorm != prefix) continue;

            // Reorder: "mega charizard y" → "charizard y mega" → normalize → "charizardymega"
            var rest = tokens[1..];
            var reordered = string.Join(" ", rest) + " " + suffix;
            var pokemon = await pokemonService.FindAsync(reordered, ct);
            if (pokemon is not null) return [(EntityType.Pokemon, pokemon)];
        }
        return [];
    }

    private static Task<object?> LookupByIdAsync(EntityType type, int id, CancellationToken ct)
    {
        // Alias resolution already handles target lookup; fallback not needed here.
        return Task.FromResult<object?>(null);
    }

    /// <summary>Returns top fuzzy candidates across all entity types for a given query.</summary>
    public async Task<IReadOnlyList<(EntityType Type, string Name, string ShowdownId)>> FuzzySearchAsync(
        string query,
        int maxPerType = 3,
        CancellationToken ct = default)
    {
        var results = new List<(EntityType, string, string)>();

        var pokemon = await pokemonService.SearchAsync(query, maxPerType, ct);
        results.AddRange(pokemon.Select(p => (EntityType.Pokemon, p.Name, p.ShowdownId)));

        var moves = await moveService.SearchAsync(query, maxPerType, ct);
        results.AddRange(moves.Select(m => (EntityType.Move, m.Name, m.ShowdownId)));

        var items = await itemService.SearchAsync(query, maxPerType, ct);
        results.AddRange(items.Select(i => (EntityType.Item, i.Name, i.ShowdownId)));

        var abilities = await abilityService.SearchAsync(query, maxPerType, ct);
        results.AddRange(abilities.Select(a => (EntityType.Ability, a.Name, a.ShowdownId)));

        return results;
    }
}
