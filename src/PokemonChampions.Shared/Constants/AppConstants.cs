namespace PokemonChampions.Shared.Constants;

public static class AppConstants
{
    public const string AppName = "pcu";
    public const string AppDisplayName = "Pokemon Champions Utils";
    public const string DataDirectoryName = "pkmn-champs";
    public const string DatabaseFileName = "data.db";

    /// <summary>Pokemon Champions stat point cap per individual stat.</summary>
    public const int MaxStatPointsPerStat = 32;

    /// <summary>Pokemon Champions total stat point cap across all six stats.</summary>
    public const int MaxTotalStatPoints = 66;

    public const int LevelCap = 50;
    public const int MaxIv = 31;
    public const int MinIv = 0;

    public const string DefaultFormat = "gen9championsregma";

    public static class SettingKeys
    {
        public const string CurrentFormat = "current_format";
        public const string OnlineMode = "online_mode";
        public const string DbVersion = "db_version";
        public const string ActiveTeamName = "active_team_name";
    }

    public static class ShowdownBaseUrl
    {
        public const string Data = "https://play.pokemonshowdown.com/data";
        public const string Pokedex = $"{Data}/pokedex.json";
        public const string Moves = $"{Data}/moves.js";
        public const string Items = $"{Data}/items.js";
        public const string Abilities = $"{Data}/abilities.js";
        public const string Learnsets = $"{Data}/learnsets.json";
    }

    public static class SmogonBaseUrl
    {
        public const string Stats = "https://www.smogon.com/stats";
    }

    public static class MunchStatsBaseUrl
    {
        public const string Base = "https://munchstats.com";
        public const string FormatsIndex = $"{Base}/";
    }
}
