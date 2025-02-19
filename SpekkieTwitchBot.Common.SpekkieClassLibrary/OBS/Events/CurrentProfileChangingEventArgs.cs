namespace SpekkieClassLibrary.OBS.Events;

public class CurrentProfileChangingEventArgs : EventArgs
{
    public CurrentProfileChangingEventArgs(string profileName)
    {
        ProfileName = profileName;
    }

    public string ProfileName { get; }
}