using PokemonChampions.Core.Domain;
using PokemonChampions.Shared.Enums;
using Spectre.Console;

namespace PokemonChampions.Cli.Rendering;

public static class MoveRenderer
{
    public static void Render(Move move)
    {
        var typeStr = TypeColors.Colorize(move.Type);
        var categoryStr = move.Category switch
        {
            MoveCategory.Physical => "[red]Physical[/]",
            MoveCategory.Special  => "[blue]Special[/]",
            MoveCategory.Status   => "[grey]Status[/]",
            _                     => move.Category.ToString()
        };

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(move.Name)}[/]");

        var meta = new List<string>
        {
            $"Type: {typeStr}",
            $"Category: {categoryStr}"
        };
        if (move.Power.HasValue)   meta.Add($"Power: [bold]{move.Power}[/]");
        if (move.Accuracy.HasValue) meta.Add($"Accuracy: [bold]{move.Accuracy}%[/]");
        meta.Add($"PP: [bold]{move.Pp}[/]");
        if (move.Priority != 0)    meta.Add($"Priority: [bold]{(move.Priority > 0 ? "+" : "")}{move.Priority}[/]");

        AnsiConsole.MarkupLine("  " + string.Join("   ", meta));

        if (!string.IsNullOrEmpty(move.ShortDesc))
            AnsiConsole.MarkupLine("  " + Markup.Escape(move.ShortDesc));

        if (!move.IsLegalInCurrentFormat)
            AnsiConsole.MarkupLine("[yellow]  ⚠ Not legal in the current format[/]");

        AnsiConsole.WriteLine();
    }
}
