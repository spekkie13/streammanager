namespace SpekkieClassLibrary.OBS.Events;

public class CurrentProfileChangingEventArgs : EventArgs
{
    public string ProfileName { get; }

    public CurrentProfileChangingEventArgs(string profileName)
    {
        ProfileName = profileName;
    }
}