using PokemonChampions.Import.Parsers;
using Xunit;

namespace PokemonChampions.Import.Tests.Parsers;

public class PokepasteParserTests
{
    private const string SamplePaste = """
        Incineroar @ Sitrus Berry
        Ability: Intimidate
        Level: 50
        EVs: 252 HP / 4 Atk / 252 SpD
        Careful Nature
        - Fake Out
        - Parting Shot
        - Flare Blitz
        - Knock Off

        Spud (Flutter Mane) @ Choice Specs
        Ability: Protosynthesis
        Level: 50
        EVs: 252 SpA / 4 SpD / 252 Spe
        Timid Nature
        IVs: 0 Atk
        - Moonblast
        - Shadow Ball
        - Mystical Fire
        - Dazzling Gleam
        """;

    [Fact]
    public void Parse_TwoMembers_ReturnsTwoResults()
    {
        var result = PokepasteParser.Parse(SamplePaste);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_FirstMember_ParsesSpeciesAndItem()
    {
        var result = PokepasteParser.Parse(SamplePaste);
        var first = result[0];
        Assert.Equal("Incineroar", first.Species);
        Assert.Equal("Sitrus Berry", first.Item);
        Assert.Null(first.Nickname);
    }

    [Fact]
    public void Parse_FirstMember_ParsesAbilityAndNature()
    {
        var result = PokepasteParser.Parse(SamplePaste);
        var first = result[0];
        Assert.Equal("Intimidate", first.Ability);
        Assert.Equal("Careful", first.Nature);
    }

    [Fact]
    public void Parse_FirstMember_ParsesFourMoves()
    {
        var result = PokepasteParser.Parse(SamplePaste);
        var first = result[0];
        Assert.Equal("Fake Out", first.Move1);
        Assert.Equal("Parting Shot", first.Move2);
        Assert.Equal("Flare Blitz", first.Move3);
        Assert.Equal("Knock Off", first.Move4);
    }

    [Fact]
    public void Parse_FirstMember_ConvertsEvsToStatPoints()
    {
        var result = PokepasteParser.Parse(SamplePaste);
        var first = result[0];
        // 252 EVs / 4 = 63 stat points
        Assert.Equal(63, first.SpHp);
        // 4 EVs / 4 = 1 stat point
        Assert.Equal(1, first.SpAtk);
        // 252 EVs / 4 = 63 stat points
        Assert.Equal(63, first.SpSpd);
        // Unspecified stats = 0
        Assert.Equal(0, first.SpDef);
        Assert.Equal(0, first.SpSpa);
        Assert.Equal(0, first.SpSpe);
    }

    [Fact]
    public void Parse_SecondMember_ParsesNicknameAndSpecies()
    {
        var result = PokepasteParser.Parse(SamplePaste);
        var second = result[1];
        Assert.Equal("Spud", second.Nickname);
        Assert.Equal("Flutter Mane", second.Species);
    }

    [Fact]
    public void Parse_SecondMember_ParsesZeroAtkIv()
    {
        var result = PokepasteParser.Parse(SamplePaste);
        var second = result[1];
        Assert.Equal(0, second.IvAtk);
        Assert.Equal(31, second.IvHp); // default
    }

    [Fact]
    public void Parse_EmptyPaste_ReturnsEmptyList()
    {
        var result = PokepasteParser.Parse(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_NicknamePokemon_WhenNoNickname_NicknameIsNull()
    {
        const string paste = "Charizard @ Life Orb\nAbility: Blaze\nTimid Nature\n- Flamethrower";
        var result = PokepasteParser.Parse(paste);
        Assert.Single(result);
        Assert.Null(result[0].Nickname);
        Assert.Equal("Charizard", result[0].Species);
    }

    [Fact]
    public void Parse_DefaultIvsAreAllThirtyOne()
    {
        const string paste = "Incineroar @ Sitrus Berry\nAbility: Intimidate\nCareful Nature\n- Fake Out";
        var result = PokepasteParser.Parse(paste);
        var member = result[0];
        Assert.Equal(31, member.IvHp);
        Assert.Equal(31, member.IvAtk);
        Assert.Equal(31, member.IvSpe);
    }
}
