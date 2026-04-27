using PokemonChampions.Core.Domain;
using Spectre.Console;

namespace PokemonChampions.Cli.Rendering;

public static class AbilityRenderer
{
    public static void Render(Ability ability)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(ability.Name)}[/]  [grey](ability)[/]");

        var desc = ability.ShortDesc ?? ability.Desc;
        if (!string.IsNullOrEmpty(desc))
            AnsiConsole.MarkupLine("  " + Markup.Escape(desc));

        if (!ability.IsLegalInCurrentFormat)
            AnsiConsole.MarkupLine("[yellow]  ⚠ Not legal in the current format[/]");

        AnsiConsole.WriteLine();
    }
}
