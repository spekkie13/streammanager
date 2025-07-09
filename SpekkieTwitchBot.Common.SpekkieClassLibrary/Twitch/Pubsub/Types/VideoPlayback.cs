using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class VideoPlayback : MessageData
{
    public VideoPlayback(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        Type = jtoken.SelectToken("type")?.ToString() switch
        {
            "stream-up" => VideoPlaybackType.StreamUp,
            "stream-down" => VideoPlaybackType.StreamDown,
            "viewcount" => VideoPlaybackType.ViewCount,
            "commercial" => VideoPlaybackType.Commercial,
            _ => Type
        };

        ServerTime = jtoken.SelectToken("server_time")?.ToString();
        switch (Type)
        {
            case VideoPlaybackType.StreamUp:
                int delay = Convert.ToInt32(jtoken.SelectToken("play_delay"));
                PlayDelay = delay;
                break;
            case VideoPlaybackType.ViewCount:
                int viewCount = Convert.ToInt32(jtoken.SelectToken("viewers"));
                Viewers = viewCount;
                break;
            case VideoPlaybackType.Commercial:
                int length = Convert.ToInt32(jtoken.SelectToken("length"));
                Length = length;
                break;
            case VideoPlaybackType.StreamDown:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public VideoPlaybackType Type { get; }
    public string? ServerTime { get; }
    public int PlayDelay { get; }
    public int Viewers { get; }
    public int Length { get; }
}