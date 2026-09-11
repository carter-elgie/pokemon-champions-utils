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
        // 252 > 32, so treated as a raw EV and converted: 252 / 8 = 31 stat points
        Assert.Equal(31, first.SpHp);
        // 4 <= 32, so treated as an already-Champions stat point value: stays 4
        Assert.Equal(4, first.SpAtk);
        // 252 > 32, so treated as a raw EV and converted: 252 / 8 = 31 stat points
        Assert.Equal(31, first.SpSpd);
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

    [Fact]
    public void Parse_NativeChampionsStatPoints_AreNotDividedDown()
    {
        // A pokepaste authored directly in Champions units (max stat point cap is 32),
        // not copied from a real Showdown export. These values must be used as-is.
        const string paste = "Sneasler @ Grassy Seed\nAbility: Unburden\nLevel: 50\nEVs: 2 HP / 32 Atk / 32 Spe\nJolly Nature\n- Dire Claw";
        var result = PokepasteParser.Parse(paste);
        var member = result[0];
        Assert.Equal(2, member.SpHp);
        Assert.Equal(32, member.SpAtk);
        Assert.Equal(32, member.SpSpe);
    }

    [Fact]
    public void Parse_SpeciesWithGenderMarker_ParsesSpeciesAndGenderSeparately()
    {
        const string paste = "Basculegion (M) @ Choice Band\nAbility: Adaptability\nAdamant Nature\n- Wave Crash";
        var result = PokepasteParser.Parse(paste);
        var member = result[0];
        Assert.Equal("Basculegion", member.Species);
        Assert.Equal("M", member.Gender);
        Assert.Null(member.Nickname);
    }

    [Fact]
    public void Parse_HyphenatedSpeciesWithGenderMarker_ParsesSpeciesAndGender()
    {
        const string paste = "Floette-Eternal (F) @ Leftovers\nAbility: Flower Veil\nBold Nature\n- Moonblast";
        var result = PokepasteParser.Parse(paste);
        var member = result[0];
        Assert.Equal("Floette-Eternal", member.Species);
        Assert.Equal("F", member.Gender);
    }

    [Fact]
    public void Parse_NicknamedSpeciesWithGenderMarker_ParsesAllThree()
    {
        const string paste = "Spud (Flutter Mane) (F) @ Choice Specs\nAbility: Protosynthesis\nTimid Nature\n- Moonblast";
        var result = PokepasteParser.Parse(paste);
        var member = result[0];
        Assert.Equal("Spud", member.Nickname);
        Assert.Equal("Flutter Mane", member.Species);
        Assert.Equal("F", member.Gender);
    }

    [Fact]
    public void Parse_SpeciesWithoutGenderMarker_GenderIsNull()
    {
        const string paste = "Charizard @ Life Orb\nAbility: Blaze\nTimid Nature\n- Flamethrower";
        var result = PokepasteParser.Parse(paste);
        Assert.Null(result[0].Gender);
    }
}
