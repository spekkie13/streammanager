namespace SpekkieClassLibrary.OBS.Events;

public class CurrentProfileChangedEventArgs : EventArgs
{
    public CurrentProfileChangedEventArgs(string profileName)
    {
        ProfileName = profileName;
    }

    public string ProfileName { get; }
}