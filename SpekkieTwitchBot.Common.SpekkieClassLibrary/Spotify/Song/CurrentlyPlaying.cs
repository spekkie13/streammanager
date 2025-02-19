namespace SpekkieClassLibrary.Spotify.Song;

public class CurrentlyPlaying
{
    public long Timestamp { get; set; }
    public Context Context { get; set; } = default!;
    public int ProgressMs { get; set; }
    public FullTrack Item { get; set; } = default!;
    public string CurrentlyPlayingType { get; set; } = default!;
    public bool IsPlaying { get; set; }
}