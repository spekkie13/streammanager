using System.Globalization;
using SpekkieClassLibrary.ClashOfClans.Ccn;
using SpekkieClassLibrary.ClashOfClans.War;

namespace SpekkieClassLibrary.Overlay;

/// <summary>
/// Pure mapping from live war data to the Live Attack Spotlight state files (war-roster.json,
/// active-player.json). No IO and no dependencies, so the resolution logic is unit-testable without a
/// running bot or an active war. The overlay/war services own the IO; this owns the shape.
/// </summary>
public static class SpotlightMapper
{
    public static WarRoster BuildRoster(RunTimeWar war, string updatedAt) => new()
    {
        UpdatedAt = updatedAt,
        WarId = war.PreparationStartTime,
        State = war.State,
        TeamSize = war.TeamSize,
        Home = BuildRosterTeam(war.Clan),
        Away = BuildRosterTeam(war.Opponent)
    };

    private static RosterTeam BuildRosterTeam(RunTimeClan clan) => new()
    {
        Name = clan.Name,
        Tag = clan.Tag,
        Players = clan.Members
            .OrderBy(m => m.MapPosition)
            .Select(m => new RosterPlayer
            {
                MapPosition = m.MapPosition,
                Tag = m.Tag,
                Name = m.Name,
                TownHall = m.TownhallLevel
            })
            .ToList()
    };

    /// <summary>
    /// Resolves a selection (team + 1-based map position) against the current war. Returns an inactive
    /// payload when there is no war, the war is not active, the selection is empty/unknown, or the
    /// position has no member — the spotlight is never half-populated.
    /// </summary>
    public static ActivePlayer BuildActivePlayer(
        RunTimeWar? war, bool isWarActive, string? team, int? position, string updatedAt,
        SpotlightCareer? career = null)
    {
        if (war == null || !isWarActive || team == null || position == null)
            return ActivePlayer.Inactive(updatedAt);

        RunTimeClan? clan = team switch
        {
            "home" => war.Clan,
            "away" => war.Opponent,
            _ => null
        };

        RunTimeMember? member = clan?.Members?.FirstOrDefault(m => m.MapPosition == position);
        if (member == null)
            return ActivePlayer.Inactive(updatedAt);

        return new ActivePlayer
        {
            Active = true,
            UpdatedAt = updatedAt,
            WarId = war.PreparationStartTime,
            Team = team,
            MapPosition = member.MapPosition,
            Tag = member.Tag,
            Name = member.Name,
            TownHall = member.TownhallLevel,
            Attack = ToSpotlightAttack(member.Attacks?.FirstOrDefault()),
            Defense = ToSpotlightAttack(member.BestOpponentAttack),
            Career = career
        };
    }

    private static SpotlightAttack? ToSpotlightAttack(RunTimeAttack? atk) => atk == null
        ? null
        : new SpotlightAttack
        {
            Stars = atk.Stars,
            Pct = atk.DestructionPercentage + "%",
            Duration = $"{(int)atk.Duration / 60}:{(int)atk.Duration % 60:D2}"
        };

    /// <summary>
    /// Maps a CCN player's basic info into the career block. Returns null when CCN has no usable data,
    /// so the field is simply omitted from active-player.json.
    /// </summary>
    public static SpotlightCareer? BuildCareer(CcnPlayerInfo? info)
    {
        if (info == null) return null;

        SpotlightStatLine? offense = ToStatLine(info.StatsOffense);
        SpotlightStatLine? defense = ToStatLine(info.StatsDefense);
        if (offense == null && defense == null) return null;

        return new SpotlightCareer { Offense = offense, Defense = defense };
    }

    private static SpotlightStatLine? ToStatLine(CcnStatLine? s)
    {
        if (s == null) return null;

        return new SpotlightStatLine
        {
            Attacks = s.Attacks,
            Triples = s.Triples,
            Doubles = s.Doubles,
            Singles = s.Singles,
            Zeros = s.Zeros,
            TripleRate = s.Attacks > 0
                ? Math.Round(100.0 * s.Triples / s.Attacks, 1).ToString("0.#", CultureInfo.InvariantCulture) + "%"
                : "0%",
            AvgStars = Math.Round(s.AvgStars, 2),
            AvgPct = Math.Round(s.AvgPerc, 1).ToString("0.#", CultureInfo.InvariantCulture) + "%",
            AvgDuration = $"{(int)s.AvgDuration / 60}:{(int)s.AvgDuration % 60:D2}"
        };
    }
}
