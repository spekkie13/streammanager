namespace SpekkieClassLibrary.Spotify.Song;

public abstract class Tracks(List<Track>? items)
{
    public List<Track>? Items { get; set; } = items;
}