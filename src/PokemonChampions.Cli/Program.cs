using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokemonChampions.Cli;
using PokemonChampions.Cli.Commands;
using PokemonChampions.Core.Formats;
using PokemonChampions.Core.Formats.Regulations;
using PokemonChampions.Data;
using Spectre.Console;

// ── DI setup ──────────────────────────────────────────────────────────────────
var services = new ServiceCollection()
    .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Warning))
    .AddPokemonChampions()
    .BuildServiceProvider();

// ── Ensure DB schema is up to date, then seed format dex lists ───────────────
using (var startupScope = services.CreateScope())
{
    var db = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Seed RegulationMA's Paldea dex allowlist from stored IsCurrentGenStandard flags.
    // Runs every launch so the check works after restarts (not just after update).
    var paldeaIds = await db.Pokemon
        .Where(p => p.IsCurrentGenStandard)
        .Select(p => p.ShowdownId)
        .ToListAsync();

    if (paldeaIds.Count > 0)
    {
        var registry = services.GetRequiredService<FormatRegistry>();
        var regMA = registry.TryGet("gen9championsregma") as RegulationMA;
        regMA?.SetPaldeaDex(new HashSet<string>(paldeaIds, StringComparer.OrdinalIgnoreCase));
    }
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// ── One-shot mode: "update [--online]" passed as CLI args ────────────────────
if (args.Length > 0 && args[0].Equals("update", StringComparison.OrdinalIgnoreCase))
{
    using var scope = services.CreateScope();
    var updateCmd = scope.ServiceProvider.GetRequiredService<UpdateCommand>();
    await updateCmd.RunFromReplAsync(args[1..], cts.Token);
    return 0;
}

// ── REPL mode: no args → interactive session ──────────────────────────────────
if (args.Length > 0)
{
    // Non-empty args that aren't "update" — dispatch a single command and exit.
    // This lets scripts pipe commands: echo "incineroar speed" | pcu
    using var scope = services.CreateScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<CommandDispatcher>();
    await dispatcher.DispatchAsync(string.Join(" ", args), cts.Token);
    return 0;
}

var repl = services.GetRequiredService<ReplLoop>();
await repl.RunAsync(cts.Token);
return 0;
