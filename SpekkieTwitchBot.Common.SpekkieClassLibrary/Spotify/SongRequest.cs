namespace SpekkieClassLibrary.Spotify;

public class SongRequest
{
    public string SpotifyId { get; set; } = null!;
    public string Requester { get; set; } = null!;
    public int ReqAmount { get; set; }
    public int PlayAmount { get; set; }
}