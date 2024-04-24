using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class VideoPlayback : MessageData
{
    public VideoPlaybackType Type { get; }
    public string ServerTime { get; }
    public int PlayDelay { get; }
    public int Viewers { get; }
    public int Length { get; }

    public VideoPlayback(string jsonStr)
    {
        JToken jtoken = JObject.Parse(jsonStr);
        switch (((object)jtoken.SelectToken("type")).ToString())
        {
            case "stream-up":
                Type = VideoPlaybackType.StreamUp;
                break;
            case "stream-down":
                Type = VideoPlaybackType.StreamDown;
                break;
            case "viewcount":
                Type = VideoPlaybackType.ViewCount;
                break;
            case "commercial":
                Type = VideoPlaybackType.Commercial;
                break;
        }

        ServerTime = ((object)jtoken.SelectToken("server_time"))?.ToString();
        switch (Type)
        {
            case VideoPlaybackType.StreamUp:
                PlayDelay = int.Parse(((object)jtoken.SelectToken("play_delay")).ToString());
                break;
            case VideoPlaybackType.ViewCount:
                Viewers = int.Parse(((object)jtoken.SelectToken("viewers")).ToString());
                break;
            case VideoPlaybackType.Commercial:
                Length = int.Parse(((object)jtoken.SelectToken("length")).ToString());
                break;
        }
    }
}