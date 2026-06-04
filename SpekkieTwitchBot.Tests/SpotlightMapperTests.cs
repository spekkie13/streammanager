using System.Text.Json;
using System.Text.Json.Serialization;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Overlay;

namespace SpekkieTwitchBot.Tests;

/// <summary>War-free tests for the spotlight mapping (selection -> active-player, war -> roster).</summary>
public class SpotlightMapperTests
{
    private const string UpdatedAt = "2026-06-04T12:34:56Z";

    // Matches the JSON the bot writes for active-player.json: camelCase, null optionals omitted.
    private static readonly JsonSerializerOptions ActivePlayerJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── BuildActivePlayer: populated ───────────────────────────────────────────

    [Fact]
    public void BuildActivePlayer_HomePosition3_ReturnsFormattedPlayer()
    {
        ActivePlayer ap = SpotlightMapper.BuildActivePlayer(SampleWar(), true, "home", 3, UpdatedAt);

        Assert.True(ap.Active);
        Assert.Equal("20260604T100000.000Z", ap.WarId);
        Assert.Equal("home", ap.Team);
        Assert.Equal(3, ap.MapPosition);
        Assert.Equal("#PLAYER3", ap.Tag);
        Assert.Equal("Player Three", ap.Name);
        Assert.Equal(16, ap.TownHall);

        Assert.NotNull(ap.Attack);
        Assert.Equal(3, ap.Attack!.Stars);
        Assert.Equal("100%", ap.Attack.Pct);
        Assert.Equal("2:45", ap.Attack.Duration);

        Assert.NotNull(ap.Defense);
        Assert.Equal("78%", ap.Defense!.Pct);
        Assert.Equal("1:50", ap.Defense.Duration);
    }

    [Fact]
    public void BuildActivePlayer_AwaySelection_ResolvesOpponent()
    {
        ActivePlayer ap = SpotlightMapper.BuildActivePlayer(SampleWar(), true, "away", 2, UpdatedAt);

        Assert.True(ap.Active);
        Assert.Equal("away", ap.Team);
        Assert.Equal("Enemy Two", ap.Name);
    }

    [Fact]
    public void BuildActivePlayer_NotYetAttacked_OmitsAttack()
    {
        // Position 5 exists but has not attacked and has not been attacked.
        ActivePlayer ap = SpotlightMapper.BuildActivePlayer(SampleWar(), true, "home", 5, UpdatedAt);

        Assert.True(ap.Active);
        Assert.Equal("Player Five", ap.Name);
        Assert.Null(ap.Attack);
        Assert.Null(ap.Defense);

        // null optionals are omitted from the JSON.
        string json = JsonSerializer.Serialize(ap, ActivePlayerJson);
        Assert.DoesNotContain("\"attack\"", json);
        Assert.DoesNotContain("\"defense\"", json);
    }

    // ── BuildActivePlayer: inactive paths ──────────────────────────────────────

    [Fact]
    public void BuildActivePlayer_NoWar_Inactive() =>
        AssertInactive(SpotlightMapper.BuildActivePlayer(null, true, "home", 1, UpdatedAt));

    [Fact]
    public void BuildActivePlayer_WarNotActive_Inactive() =>
        AssertInactive(SpotlightMapper.BuildActivePlayer(SampleWar(), false, "home", 1, UpdatedAt));

    [Fact]
    public void BuildActivePlayer_NoSelection_Inactive() =>
        AssertInactive(SpotlightMapper.BuildActivePlayer(SampleWar(), true, null, null, UpdatedAt));

    [Fact]
    public void BuildActivePlayer_UnknownTeam_Inactive() =>
        AssertInactive(SpotlightMapper.BuildActivePlayer(SampleWar(), true, "enemy", 1, UpdatedAt));

    [Fact]
    public void BuildActivePlayer_MissingPosition_Inactive() =>
        AssertInactive(SpotlightMapper.BuildActivePlayer(SampleWar(), true, "home", 9, UpdatedAt));

    private static void AssertInactive(ActivePlayer ap)
    {
        Assert.False(ap.Active);
        Assert.Equal(UpdatedAt, ap.UpdatedAt);
        Assert.Null(ap.Team);
        Assert.Null(ap.Attack);

        // Inactive serializes to just { active, updatedAt }.
        string json = JsonSerializer.Serialize(ap, ActivePlayerJson);
        Assert.DoesNotContain("\"team\"", json);
        Assert.DoesNotContain("\"mapPosition\"", json);
    }

    // ── BuildRoster ────────────────────────────────────────────────────────────

    [Fact]
    public void BuildRoster_ProducesBothTeamsOrderedByPosition()
    {
        WarRoster roster = SpotlightMapper.BuildRoster(SampleWar(), UpdatedAt);

        Assert.Equal(5, roster.TeamSize);
        Assert.Equal("inWar", roster.State);
        Assert.Equal("My Clan", roster.Home.Name);
        Assert.Equal(5, roster.Home.Players.Count);
        Assert.Equal(5, roster.Away.Players.Count);

        // Stable identity only — ordered by map position, with town halls.
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, roster.Home.Players.Select(p => p.MapPosition).ToArray());
        Assert.Equal("Player Three", roster.Home.Players[2].Name);
        Assert.Equal(16, roster.Home.Players[2].TownHall);
    }

    // ── Sample 5v5 war ─────────────────────────────────────────────────────────

    private static RunTimeWar SampleWar() => new()
    {
        State = "inWar",
        TeamSize = 5,
        AttacksPerMember = 1,
        PreparationStartTime = "20260604T100000.000Z",
        Clan = new RunTimeClan
        {
            Name = "My Clan",
            Tag = "#HOME",
            Stars = 9,
            DestructionPercentage = 91.5,
            Members =
            [
                Member(1, "#PLAYER1", "Player One", 16, attackStars: 2, attackPct: 95, attackSecs: 130),
                Member(2, "#PLAYER2", "Player Two", 15, attackStars: 1, attackPct: 60, attackSecs: 95),
                // Position 3 = the plan example: 3-star attack + a defense logged against the base.
                Member(3, "#PLAYER3", "Player Three", 16,
                    attackStars: 3, attackPct: 100, attackSecs: 165,
                    defStars: 2, defPct: 78, defSecs: 110),
                Member(4, "#PLAYER4", "Player Four", 15, attackStars: 0, attackPct: 12, attackSecs: 40),
                Member(5, "#PLAYER5", "Player Five", 14) // not yet attacked
            ]
        },
        Opponent = new RunTimeClan
        {
            Name = "Enemy Clan",
            Tag = "#AWAY",
            Stars = 6,
            DestructionPercentage = 70.0,
            Members =
            [
                Member(1, "#ENEMY1", "Enemy One", 16, attackStars: 2, attackPct: 88, attackSecs: 140),
                Member(2, "#ENEMY2", "Enemy Two", 16, attackStars: 3, attackPct: 100, attackSecs: 150),
                Member(3, "#ENEMY3", "Enemy Three", 15),
                Member(4, "#ENEMY4", "Enemy Four", 15),
                Member(5, "#ENEMY5", "Enemy Five", 14)
            ]
        }
    };

    private static RunTimeMember Member(
        int position, string tag, string name, int townHall,
        int? attackStars = null, double attackPct = 0, double attackSecs = 0,
        int? defStars = null, double defPct = 0, double defSecs = 0) => new()
    {
        MapPosition = position,
        Tag = tag,
        Name = name,
        TownhallLevel = townHall,
        Attacks = attackStars == null
            ? null
            : [new RunTimeAttack { Stars = attackStars.Value, DestructionPercentage = attackPct, Duration = attackSecs }],
        BestOpponentAttack = defStars == null
            ? null
            : new RunTimeAttack { Stars = defStars.Value, DestructionPercentage = defPct, Duration = defSecs }
    };
}
