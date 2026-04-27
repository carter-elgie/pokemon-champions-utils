using PokemonChampions.Core.Domain;
using Spectre.Console;

namespace PokemonChampions.Cli.Rendering;

public static class ItemRenderer
{
    public static void Render(Item item)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(item.Name)}[/]  [grey](item)[/]");

        if (item.IsMegaStone && item.MegaStoneFor is not null)
            AnsiConsole.MarkupLine($"  [grey]Mega Stone for:[/] {Markup.Escape(item.MegaStoneFor)}");

        var desc = item.ShortDesc ?? item.Desc;
        if (!string.IsNullOrEmpty(desc))
            AnsiConsole.MarkupLine("  " + Markup.Escape(desc));

        if (!item.IsLegalInCurrentFormat)
            AnsiConsole.MarkupLine("[yellow]  ⚠ Not legal in the current format[/]");

        AnsiConsole.WriteLine();
    }
}
