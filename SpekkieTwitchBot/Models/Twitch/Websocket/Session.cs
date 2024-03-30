namespace SpekkieTwitchBot.Models.Twitch.Websocket;

public class Session
{
    public string id;
    public string status;
    public string connected_at;
    public int keepalive_timeout_seconds;
    public string? reconnect_url;
}