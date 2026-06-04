using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Overlay;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.Systems.Overlay;
using Xunit.Abstractions;

namespace SpekkieTwitchBot.Tests;

/// <summary>
/// Opt-in local harness for testing the Live Attack Spotlight WITHOUT a live war. It reuses the real
/// SpotlightSelectionReader and SpotlightMapper and writes the real war-roster.json + active-player.json
/// into the bot's Output folder from a canned 5v5 war, resolving whatever is currently in
/// Settings/spotlight-selection.txt.
///
/// It is gated by the SPOTLIGHT_HARNESS=1 environment variable, so a normal `dotnet test` / CI run skips
/// it (returns immediately, no files written). Run it on demand:
///
///   $env:SPOTLIGHT_HARNESS = "1"
///   Set-Content "$env:USERPROFILE\Desktop\SpekkieTwitchBot\Settings\spotlight-selection.txt" "home:3"
///   dotnet test --filter "FullyQualifiedName~SpotlightLocalHarness"
///   # inspect Output\ClashOfClans\war-roster.json and active-player.json, change the selection, re-run
///
/// Point BOT_BASE_DIR at a scratch folder to avoid writing into your real Output.
/// </summary>
public class SpotlightLocalHarness(ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions RosterJson =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private static readonly JsonSerializerOptions ActivePlayerJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void WritesRealFilesFromCannedWar()
    {
        if (Environment.GetEnvironmentVariable("SPOTLIGHT_HARNESS") != "1")
            return; // opt-in only — normal test runs do nothing.

        string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        RunTimeWar war = CannedWar();

        string cocDir = Path.Combine(BotPaths.BaseDir, "Output", "ClashOfClans");
        Directory.CreateDirectory(cocDir);

        // war-roster.json — exactly what WarService publishes (same SpotlightMapper.BuildRoster).
        string rosterPath = Path.Combine(cocDir, "war-roster.json");
        File.WriteAllText(rosterPath, JsonSerializer.Serialize(SpotlightMapper.BuildRoster(war, now), RosterJson));

        // active-player.json — resolve the REAL selection file with the REAL reader, then the REAL mapper.
        var reader = new SpotlightSelectionReader(new Mock<Logger>(MockBehavior.Loose, null!).Object);
        SpotlightSelection? selection = reader.Read();
        ActivePlayer activePlayer =
            SpotlightMapper.BuildActivePlayer(war, true, selection?.Team, selection?.Position, now);

        string activePath = Path.Combine(cocDir, "active-player.json");
        File.WriteAllText(activePath, JsonSerializer.Serialize(activePlayer, ActivePlayerJson));

        output.WriteLine($"Selection read: {(selection == null ? "(off)" : $"{selection.Team}:{selection.Position}")}");
        output.WriteLine($"war-roster.json    -> {rosterPath}");
        output.WriteLine($"active-player.json -> {activePath}");
        output.WriteLine(File.ReadAllText(activePath));
    }

    private static RunTimeWar CannedWar() => new()
    {
        State = "inWar",
        TeamSize = 5,
        AttacksPerMember = 1,
        PreparationStartTime = "20260604T100000.000Z",
        Clan = new RunTimeClan
        {
            Name = "My Clan", Tag = "#HOME", Stars = 9, DestructionPercentage = 91.5,
            Members =
            [
                Member(1, "#PLAYER1", "Player One", 16, 2, 95, 130),
                Member(2, "#PLAYER2", "Player Two", 15, 1, 60, 95),
                Member(3, "#PLAYER3", "Player Three", 16, 3, 100, 165, 2, 78, 110),
                Member(4, "#PLAYER4", "Player Four", 15, 0, 12, 40),
                Member(5, "#PLAYER5", "Player Five", 14)
            ]
        },
        Opponent = new RunTimeClan
        {
            Name = "Enemy Clan", Tag = "#AWAY", Stars = 6, DestructionPercentage = 70.0,
            Members =
            [
                Member(1, "#ENEMY1", "Enemy One", 16, 2, 88, 140),
                Member(2, "#ENEMY2", "Enemy Two", 16, 3, 100, 150),
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
        MapPosition = position, Tag = tag, Name = name, TownhallLevel = townHall,
        Attacks = attackStars == null
            ? null
            : [new RunTimeAttack { Stars = attackStars.Value, DestructionPercentage = attackPct, Duration = attackSecs }],
        BestOpponentAttack = defStars == null
            ? null
            : new RunTimeAttack { Stars = defStars.Value, DestructionPercentage = defPct, Duration = defSecs }
    };
}
