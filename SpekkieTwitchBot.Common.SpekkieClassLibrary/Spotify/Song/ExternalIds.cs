namespace SpekkieClassLibrary.Spotify.Song;

public abstract class ExternalIds(string isrc)
{
    public string Isrc { get; set; } = isrc;
}