namespace SpekkieClassLibrary.Overlay;

/// <summary>
/// Stable war lineup published as war-roster.json. Identity only — no live attack stats, which
/// belong in active-player.json. Lets the operator/StreamDeck map a button (team + map position)
/// to a player.
/// </summary>
public class WarRoster
{
    public string UpdatedAt { get; init; } = "";
    public string WarId { get; init; } = "";
    public string State { get; init; } = "";
    public int TeamSize { get; init; }
    public RosterTeam Home { get; init; } = new();
    public RosterTeam Away { get; init; } = new();
}
