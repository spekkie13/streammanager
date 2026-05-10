namespace SpekkieClassLibrary.Events;

public record WarStateChangedEvent(string State, string? ClanName, string? OpponentName)
{
    public bool IsActive => State is "preparation" or "inWar";
}
