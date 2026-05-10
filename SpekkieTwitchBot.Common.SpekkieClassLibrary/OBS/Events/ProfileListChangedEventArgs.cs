namespace SpekkieClassLibrary.OBS.Events;

public class ProfileListChangedEventArgs : EventArgs
{
    public ProfileListChangedEventArgs(List<string> profiles)
    {
        Profiles = profiles;
    }

    public List<string> Profiles { get; }
}