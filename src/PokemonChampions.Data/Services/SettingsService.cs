using Microsoft.EntityFrameworkCore;
using PokemonChampions.Core.Services;
using PokemonChampions.Data.Entities;

namespace PokemonChampions.Data.Services;

public class SettingsService(AppDbContext db) : ISettingsService
{
    public async Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        (await db.AppSettings.FindAsync([key], ct))?.Value;

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        var setting = await db.AppSettings.FindAsync([key], ct);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSettingEntity { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken ct = default)
    {
        var v = await GetAsync(key, ct);
        return v is null ? defaultValue : v is "true" or "1" or "yes";
    }

    public async Task SetBoolAsync(string key, bool value, CancellationToken ct = default) =>
        await SetAsync(key, value ? "true" : "false", ct);
}
