namespace SpekkieTwitchBot.Models.OBS.Events;

public class CurrentProfileChangingEventArgs : EventArgs
{
    public string ProfileName { get; }

    public CurrentProfileChangingEventArgs(string profileName)
    {
        ProfileName = profileName;
    }
}