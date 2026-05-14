using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PokemonChampions.Data;
using PokemonChampions.Data.Entities;
using PokemonChampions.Import.Parsers;
using PokemonChampions.Shared.Extensions;

namespace PokemonChampions.Import.Importers;

/// <summary>
/// Downloads and imports the static data files from Pokemon Showdown into the local database.
/// Covers Pokemon (pokedex), moves, items, abilities, and learnsets.
/// </summary>
public class StaticDataImporter(AppDbContext db, HttpClient http, ILogger<StaticDataImporter> logger)
{
    public async Task ImportAllAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Fetching Pokemon data...");
        await ImportPokemonAsync(ct);

        progress?.Report("Fetching move data...");
        await ImportMovesAsync(ct);

        progress?.Report("Fetching item data...");
        await ImportItemsAsync(ct);

        progress?.Report("Fetching ability data...");
        await ImportAbilitiesAsync(ct);

        progress?.Report("Fetching learnset data...");
        await ImportLearnsetsAsync(ct);

        progress?.Report("Done.");
    }

    private async Task ImportPokemonAsync(CancellationToken ct)
    {
        var json = await http.GetStringAsync("https://play.pokemonshowdown.com/data/pokedex.json", ct);
        using var doc = ShowdownJsParser.ParseJson(json);

        var now = DateTime.UtcNow;
        int upserted = 0;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var id = prop.Name; // showdown id, e.g. "charizard"
            var obj = prop.Value;

            if (!obj.TryGetProperty("num", out var numProp) || numProp.GetInt32() <= 0)
                continue; // skip dummy entries and non-standard Pokemon

            var num = numProp.GetInt32();

            // Base stats
            int hp = 0, atk = 0, def = 0, spa = 0, spd = 0, spe = 0;
            if (obj.TryGetProperty("baseStats", out var bs))
            {
                hp  = bs.TryGetProperty("hp",  out var v) ? v.GetInt32() : 0;
                atk = bs.TryGetProperty("atk", out v) ? v.GetInt32() : 0;
                def = bs.TryGetProperty("def", out v) ? v.GetInt32() : 0;
                spa = bs.TryGetProperty("spa", out v) ? v.GetInt32() : 0;
                spd = bs.TryGetProperty("spd", out v) ? v.GetInt32() : 0;
                spe = bs.TryGetProperty("spe", out v) ? v.GetInt32() : 0;
            }

            // Types
            string? type1 = null, type2 = null;
            if (obj.TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array)
            {
                var typeArr = types.EnumerateArray().Select(t => t.GetString()).ToList();
                type1 = typeArr.ElementAtOrDefault(0);
                type2 = typeArr.Count > 1 ? typeArr[1] : null;
            }

            // Abilities
            string? ab0 = null, ab1 = null, abH = null;
            if (obj.TryGetProperty("abilities", out var abils))
            {
                ab0 = abils.TryGetProperty("0", out var a) ? a.GetString() : null;
                ab1 = abils.TryGetProperty("1", out a) ? a.GetString() : null;
                abH = abils.TryGetProperty("H", out a) ? a.GetString() : null;
            }

            string name = obj.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? id : id;
            bool isMega = name.Contains("-Mega") || name.Contains("-Mega-");
            string? baseForm = obj.TryGetProperty("baseSpecies", out var baseProp) ? baseProp.GetString()?.ToShowdownId() : null;
            bool canEvolve = obj.TryGetProperty("evos", out var evos)
                && evos.ValueKind == JsonValueKind.Array
                && evos.GetArrayLength() > 0;

            var entity = await db.Pokemon.FirstOrDefaultAsync(p => p.ShowdownId == id, ct)
                ?? new PokemonEntity { ShowdownId = id };

            entity.NormalizedId = id.ToNormalizedId();
            entity.Name = name;
            entity.BaseHp = hp;
            entity.BaseAtk = atk;
            entity.BaseDef = def;
            entity.BaseSpa = spa;
            entity.BaseSpd = spd;
            entity.BaseSpe = spe;
            entity.Type1 = type1;
            entity.Type2 = type2;
            entity.Ability0 = ab0;
            entity.Ability1 = ab1;
            entity.AbilityH = abH;
            entity.IsMega = isMega;
            entity.BaseFormShowdownId = baseForm != id ? baseForm : null;
            entity.CanEvolve = canEvolve;
            entity.IsCurrentGenStandard = !obj.TryGetProperty("isNonstandard", out var ns) || ns.GetString() != "Past";
            entity.UpdatedAt = now;

            if (entity.Id == 0)
                db.Pokemon.Add(entity);

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Upserted {Count} Pokemon entries.", upserted);
    }

    private async Task ImportMovesAsync(CancellationToken ct)
    {
        var raw = await http.GetStringAsync("https://play.pokemonshowdown.com/data/moves.js", ct);
        using var doc = ShowdownJsParser.Parse(raw, "BattleMovedex");

        var now = DateTime.UtcNow;
        int upserted = 0;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var id = prop.Name;
            var obj = prop.Value;

            if (!obj.TryGetProperty("num", out var numProp) || numProp.GetInt32() <= 0) continue;

            string name = obj.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
            string? type = obj.TryGetProperty("type", out var t) ? t.GetString() : null;
            string? category = obj.TryGetProperty("category", out var cat) ? cat.GetString() : null;
            int? power = obj.TryGetProperty("basePower", out var bp) && bp.GetInt32() > 0 ? bp.GetInt32() : null;
            int? accuracy = obj.TryGetProperty("accuracy", out var acc) && acc.ValueKind == JsonValueKind.Number ? acc.GetInt32() : null;
            int pp = obj.TryGetProperty("pp", out var ppProp) ? ppProp.GetInt32() : 0;
            int priority = obj.TryGetProperty("priority", out var pri) ? pri.GetInt32() : 0;
            string? target = obj.TryGetProperty("target", out var tgt) ? tgt.GetString() : null;
            string? shortDesc = obj.TryGetProperty("shortDesc", out var sd) ? sd.GetString() : null;
            string? desc = obj.TryGetProperty("desc", out var d) ? d.GetString() : null;

            string? flags = null;
            if (obj.TryGetProperty("flags", out var flagsEl))
                flags = flagsEl.GetRawText();

            var entity = await db.Moves.FirstOrDefaultAsync(m => m.ShowdownId == id, ct)
                ?? new MoveEntity { ShowdownId = id };

            entity.NormalizedId = id.ToNormalizedId();
            entity.Name = name;
            entity.Type = type;
            entity.Category = category;
            entity.Power = power;
            entity.Accuracy = accuracy;
            entity.Pp = pp;
            entity.Priority = priority;
            entity.Target = target;
            entity.ShortDesc = shortDesc;
            entity.Desc = desc;
            entity.Flags = flags;
            entity.UpdatedAt = now;

            if (entity.Id == 0)
                db.Moves.Add(entity);

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Upserted {Count} move entries.", upserted);
    }

    private async Task ImportItemsAsync(CancellationToken ct)
    {
        var raw = await http.GetStringAsync("https://play.pokemonshowdown.com/data/items.js", ct);
        using var doc = ShowdownJsParser.Parse(raw, "BattleItems");

        var now = DateTime.UtcNow;
        int upserted = 0;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var id = prop.Name;
            var obj = prop.Value;

            if (!obj.TryGetProperty("num", out var numProp) || numProp.GetInt32() <= 0) continue;

            string name = obj.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
            string? shortDesc = obj.TryGetProperty("shortDesc", out var sd) ? sd.GetString() : null;
            string? desc = obj.TryGetProperty("desc", out var d) ? d.GetString() : null;
            bool isMega = obj.TryGetProperty("megaStone", out _);
            string? megaStoneFor = obj.TryGetProperty("megaEvolves", out var me) ? me.GetString()?.ToShowdownId() : null;

            var entity = await db.Items.FirstOrDefaultAsync(i => i.ShowdownId == id, ct)
                ?? new ItemEntity { ShowdownId = id };

            entity.NormalizedId = id.ToNormalizedId();
            entity.Name = name;
            entity.ShortDesc = shortDesc;
            entity.Desc = desc;
            entity.IsMegaStone = isMega;
            entity.MegaStoneFor = megaStoneFor;
            entity.UpdatedAt = now;

            if (entity.Id == 0)
                db.Items.Add(entity);

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Upserted {Count} item entries.", upserted);
    }

    private async Task ImportAbilitiesAsync(CancellationToken ct)
    {
        var raw = await http.GetStringAsync("https://play.pokemonshowdown.com/data/abilities.js", ct);
        using var doc = ShowdownJsParser.Parse(raw, "BattleAbilities");

        var now = DateTime.UtcNow;
        int upserted = 0;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var id = prop.Name;
            var obj = prop.Value;

            if (!obj.TryGetProperty("num", out var numProp) || numProp.GetInt32() <= 0) continue;

            string name = obj.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
            string? shortDesc = obj.TryGetProperty("shortDesc", out var sd) ? sd.GetString() : null;
            string? desc = obj.TryGetProperty("desc", out var d) ? d.GetString() : null;

            var entity = await db.Abilities.FirstOrDefaultAsync(a => a.ShowdownId == id, ct)
                ?? new AbilityEntity { ShowdownId = id };

            entity.NormalizedId = id.ToNormalizedId();
            entity.Name = name;
            entity.ShortDesc = shortDesc;
            entity.Desc = desc;
            entity.UpdatedAt = now;

            if (entity.Id == 0)
                db.Abilities.Add(entity);

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Upserted {Count} ability entries.", upserted);
    }

    private async Task ImportLearnsetsAsync(CancellationToken ct)
    {
        var json = await http.GetStringAsync("https://play.pokemonshowdown.com/data/learnsets.json", ct);
        using var doc = ShowdownJsParser.ParseJson(json);

        // Build lookup maps to avoid repeated DB queries
        var pokemonMap = await db.Pokemon.AsNoTracking()
            .ToDictionaryAsync(p => p.ShowdownId, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);
        var moveMap = await db.Moves.AsNoTracking()
            .ToDictionaryAsync(m => m.ShowdownId, m => m.Id, StringComparer.OrdinalIgnoreCase, ct);

        // Clear existing learnset data to re-import cleanly
        await db.Learnsets.ExecuteDeleteAsync(ct);

        var batch = new List<LearnsetEntity>();
        const int batchSize = 500;

        foreach (var pokemonProp in doc.RootElement.EnumerateObject())
        {
            var pokemonId = pokemonProp.Name;
            if (!pokemonMap.TryGetValue(pokemonId, out int dbPokemonId)) continue;

            if (!pokemonProp.Value.TryGetProperty("learnset", out var learnset)) continue;

            // Deduplicate within each pokemon to avoid hitting the unique index constraint.
            var seen = new HashSet<(int moveId, int gen, string method)>();

            foreach (var moveProp in learnset.EnumerateObject())
            {
                var moveId = moveProp.Name;
                if (!moveMap.TryGetValue(moveId, out int dbMoveId)) continue;

                // Each entry is an array of strings like ["9L1", "9M", "8E", "8S0"].
                // Parse by consuming leading digits as generation, then take next char as method.
                // "9M" → gen=9, method="M"
                // "9L1" → gen=9, method="L"  (level-up; suffix after method char is ignored)
                // "8S0" → gen=8, method="S"  (event; suffix is event index)
                foreach (var entry in moveProp.Value.EnumerateArray())
                {
                    var entryStr = entry.GetString();
                    if (entryStr == null || entryStr.Length < 2) continue;

                    int methodStart = 0;
                    while (methodStart < entryStr.Length && char.IsDigit(entryStr[methodStart]))
                        methodStart++;

                    if (methodStart == 0 || methodStart >= entryStr.Length) continue;
                    if (!int.TryParse(entryStr[..methodStart], out int gen)) continue;

                    var method = entryStr[methodStart].ToString();

                    if (!seen.Add((dbMoveId, gen, method))) continue;

                    batch.Add(new LearnsetEntity
                    {
                        PokemonId = dbPokemonId,
                        MoveId = dbMoveId,
                        Generation = gen,
                        Method = method,
                    });

                    if (batch.Count >= batchSize)
                    {
                        await db.Learnsets.AddRangeAsync(batch, ct);
                        await db.SaveChangesAsync(ct);
                        batch.Clear();
                    }
                }
            }
        }

        if (batch.Count > 0)
        {
            await db.Learnsets.AddRangeAsync(batch, ct);
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("Learnset import complete.");
    }

}
