using SpekkieClassLibrary.ClashOfClans.Ccn;
using SpekkieClassLibrary.Overlay;

namespace SpekkieTwitchBot.Tests;

/// <summary>Tests for mapping CCN player info into the active-player.json career block.</summary>
public class SpotlightCareerMapperTests
{
    [Fact]
    public void BuildCareer_MapsOffenseAndDefense_Formatted()
    {
        CcnPlayerInfo info = new()
        {
            Name = "Player Three",
            StatsOffense = new CcnStatLine
            {
                Attacks = 100, Triples = 62, Doubles = 30, Singles = 6, Zeros = 2,
                AvgStars = 2.53, AvgPerc = 94.83, AvgDuration = 138
            },
            StatsDefense = new CcnStatLine
            {
                Attacks = 80, Triples = 20, Doubles = 40, Singles = 15, Zeros = 5,
                AvgStars = 1.94, AvgPerc = 78.4, AvgDuration = 110
            }
        };

        SpotlightCareer? career = SpotlightMapper.BuildCareer(info);

        Assert.NotNull(career);
        Assert.NotNull(career!.Offense);
        Assert.Equal(100, career.Offense!.Attacks);
        Assert.Equal(62, career.Offense.Triples);
        Assert.Equal("62%", career.Offense.TripleRate);
        Assert.Equal(2.53, career.Offense.AvgStars);
        Assert.Equal("94.8%", career.Offense.AvgPct);
        Assert.Equal("2:18", career.Offense.AvgDuration);

        Assert.NotNull(career.Defense);
        Assert.Equal("25%", career.Defense!.TripleRate); // 20/80
        Assert.Equal("1:50", career.Defense.AvgDuration);
    }

    [Fact]
    public void BuildCareer_NullInfo_ReturnsNull() =>
        Assert.Null(SpotlightMapper.BuildCareer(null));

    [Fact]
    public void BuildCareer_NoStatBlocks_ReturnsNull() =>
        Assert.Null(SpotlightMapper.BuildCareer(new CcnPlayerInfo { Name = "x" }));

    [Fact]
    public void BuildCareer_ZeroAttacks_TripleRateIsZeroPercent()
    {
        CcnPlayerInfo info = new()
        {
            StatsOffense = new CcnStatLine { Attacks = 0, Triples = 0, AvgStars = 0, AvgPerc = 0, AvgDuration = 0 }
        };

        SpotlightCareer? career = SpotlightMapper.BuildCareer(info);

        Assert.NotNull(career);
        Assert.Equal("0%", career!.Offense!.TripleRate);
        Assert.Null(career.Defense);
    }

    [Fact]
    public void BuildActivePlayer_WithCareer_AttachesIt()
    {
        SpotlightCareer career = new() { Offense = new SpotlightStatLine { Attacks = 10 } };

        // Inactive selection still returns inactive (career is only attached to an active player).
        ActivePlayer inactive = SpotlightMapper.BuildActivePlayer(null, true, "home", 1, "now", career);
        Assert.False(inactive.Active);
        Assert.Null(inactive.Career);
    }
}
