using Microsoft.Extensions.DependencyInjection;
using PokemonChampions.Cli.Commands;
using Spectre.Console;

namespace PokemonChampions.Cli;

/// <summary>
/// Runs the interactive REPL (read–evaluate–print loop).
/// Each input line gets its own DI scope so EF Core's DbContext is fresh per command.
/// </summary>
public class ReplLoop(IServiceScopeFactory scopeFactory)
{
    private const string Prompt = "pcu> ";

    public async Task RunAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[bold]Pokemon Champions Utils[/]  [grey]Type 'help' for commands, 'exit' to quit.[/]");
        AnsiConsole.WriteLine();

        while (!ct.IsCancellationRequested)
        {
            Console.Write(Prompt);

            string? line;
            try
            {
                line = Console.ReadLine();
            }
            catch (IOException)
            {
                break;
            }

            if (line is null) break;

            bool keepGoing;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<CommandDispatcher>();
                keepGoing = await dispatcher.DispatchAsync(line, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                keepGoing = true;
            }

            if (!keepGoing) break;
        }

        AnsiConsole.MarkupLine("[grey]Goodbye.[/]");
    }
}
