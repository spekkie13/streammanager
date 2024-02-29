namespace SpekkieTwitchBot.Constants;

public static class SpotifyConstants
{
    public static string CurrentlyPlayingUrl => "https://api.spotify.com/v1/me/player/currently-playing";
    public static string PausePlayerUrl => "https://api.spotify.com/v1/me/player/pause";
    public static string StartPlayerUrl => "https://api.spotify.com/v1/me/player/play";
    public static string SkipNextUrl => "https://api.spotify.com/v1/me/player/next";
    public static string SkipPrevUrl => "https://api.spotify.com/v1/me/player/previous";
    public static string AddToQueueUrl => "https://api.spotify.com/v1/me/player/queue?uri=";
    public static string GetQueueUrl => "https://api.spotify.com/v1/me/player/queue";
    public static string TokenUrl => "https://accounts.spotify.com/api/token";
}