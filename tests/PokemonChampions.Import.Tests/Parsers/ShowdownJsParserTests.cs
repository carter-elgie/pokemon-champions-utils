using System.Text.Json;
using PokemonChampions.Import.Parsers;
using Xunit;

namespace PokemonChampions.Import.Tests.Parsers;

public class ShowdownJsParserTests
{
    [Fact]
    public void Parse_ValidJsExport_ReturnsJsonDocument()
    {
        const string content = """exports.BattleMovedex = {"tackle":{"num":33,"name":"Tackle"}};""";
        using var doc = ShowdownJsParser.Parse(content, "BattleMovedex");
        Assert.True(doc.RootElement.TryGetProperty("tackle", out var tackle));
        Assert.Equal("Tackle", tackle.GetProperty("name").GetString());
    }

    [Fact]
    public void Parse_AutoDetect_FindsExportWithoutExplicitName()
    {
        const string content = """exports.BattleItems = {"leftovers":{"num":234,"name":"Leftovers"}};""";
        using var doc = ShowdownJsParser.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("leftovers", out _));
    }

    [Fact]
    public void Parse_WrongExportName_ThrowsFormatException()
    {
        const string content = """exports.BattleMovedex = {};""";
        Assert.Throws<FormatException>(() => ShowdownJsParser.Parse(content, "BattleItems"));
    }

    [Fact]
    public void ParseJson_ValidJson_ReturnsDocument()
    {
        const string json = """{"pikachu":{"num":25,"name":"Pikachu"}}""";
        using var doc = ShowdownJsParser.ParseJson(json);
        Assert.True(doc.RootElement.TryGetProperty("pikachu", out _));
    }
}
