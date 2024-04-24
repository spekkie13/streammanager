namespace SpekkieClassLibrary.OBS.Events;

public class ProfileListChangedEventArgs : EventArgs
{
    public List<string> Profiles { get; }
    public ProfileListChangedEventArgs(List<string> profiles)
    {
        Profiles = profiles;
    }
}