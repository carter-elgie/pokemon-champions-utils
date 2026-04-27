using PokemonChampions.Shared.Constants;

namespace PokemonChampions.Data;

/// <summary>
/// Resolves the platform-appropriate path for the local SQLite database.
/// Linux: ~/.config/pkmn-champs/data.db
/// Windows: %APPDATA%\pkmn-champs\data.db
/// </summary>
public static class DatabasePathProvider
{
    public static string GetDatabasePath()
    {
        var baseDir = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        var appDir = Path.Combine(baseDir, AppConstants.DataDirectoryName);
        Directory.CreateDirectory(appDir);
        return Path.Combine(appDir, AppConstants.DatabaseFileName);
    }
}
