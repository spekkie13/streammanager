namespace SpekkieClassLibrary.OBS.Events;

public class CurrentProfileChangedEventArgs : EventArgs
{
    public string ProfileName { get; }

    public CurrentProfileChangedEventArgs(string profileName)
    {
        ProfileName = profileName;
    }
}