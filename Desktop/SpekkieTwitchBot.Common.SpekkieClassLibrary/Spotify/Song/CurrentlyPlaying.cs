namespace SpekkieClassLibrary.Spotify.Song;

public class CurrentlyPlaying
{
    public long Timestamp { get; set; }
    public Context Context { get; set; } = null!;
    public int ProgressMs { get; set; }
    public FullTrack Item { get; set; } = null!;
    public string CurrentlyPlayingType { get; set; } = null!;
    public bool IsPlaying { get; set; }
}