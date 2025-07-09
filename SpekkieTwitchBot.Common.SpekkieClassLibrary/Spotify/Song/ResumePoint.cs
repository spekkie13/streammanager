namespace SpekkieClassLibrary.Spotify.Song;

public abstract class ResumePoint
{
    public bool FullyPlayed { get; set; }
    public int ResumePositionMs { get; set; }
}