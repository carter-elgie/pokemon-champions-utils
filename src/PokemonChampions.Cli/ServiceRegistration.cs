using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokemonChampions.Cli.Commands;
using PokemonChampions.Cli.Parsing;
using PokemonChampions.Cli.Services;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Formats.Regulations;
using PokemonChampions.Core.Services;
using PokemonChampions.Data;
using PokemonChampions.Data.Services;
using PokemonChampions.Import.Importers;
using PokemonChampions.Import.Sources;
using PokemonChampions.Shared.Constants;

namespace PokemonChampions.Cli;

/// <summary>Configures the DI container for the CLI application.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddPokemonChampions(this IServiceCollection services)
    {
        // Database — scoped so each REPL line gets a fresh DbContext
        var dbPath = DatabasePathProvider.GetDatabasePath();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // HTTP — typed clients are transient
        services.AddHttpClient<StaticDataImporter>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PokemonChampionsUtils/1.0");
        });
        services.AddHttpClient<MunchStatsSource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PokemonChampionsUtils/1.0");
        });

        // Formats — singleton (immutable format definitions)
        services.AddSingleton<FormatRegistry>(sp =>
        {
            var registry = new FormatRegistry();
            registry.Register(new RegulationMA());
            registry.Register(new RegulationMC());
            return registry;
        });

        // Data services — scoped to match AppDbContext lifetime
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAliasService, AliasService>();
        services.AddScoped<IPokemonService, PokemonService>();
        services.AddScoped<IMoveService, MoveService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IAbilityService, AbilityService>();

        // Team service lives in Cli because it depends on both Data and Import
        services.AddScoped<ITeamService, TeamService>();

        // Usage stats — importer is scoped (wraps MunchStatsSource + DbContext)
        services.AddScoped<UsageStatsImporter>();
        services.AddScoped<IUsageStatsService, UsageStatsService>();

        // CLI layer — scoped (depend on scoped data services)
        services.AddScoped<MultiWordNameParser>();
        services.AddScoped<UpdateCommand>();
        services.AddScoped<ConfigCommand>();
        services.AddScoped<TeamsCommand>();
        services.AddScoped<CommandDispatcher>();

        // REPL loop — singleton (creates its own scopes per command)
        services.AddSingleton<ReplLoop>();

        return services;
    }
}
