#nullable disable
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class ChannelBitsEvents : MessageData
{
    public ChannelBitsEvents(string jsonStr)
    {
        var jobject = JObject.Parse(jsonStr);
        Username = jobject.SelectToken("data")?.SelectToken("user_name")?.ToString();
        ChannelName = jobject.SelectToken("data")?.SelectToken("channel_name")?.ToString();
        UserId = jobject.SelectToken("data")?.SelectToken("user_id")?.ToString();
        ChannelId = jobject.SelectToken("data")?.SelectToken("channel_id")?.ToString();
        Time = jobject.SelectToken("data")?.SelectToken("time")?.ToString();
        ChatMessage = jobject.SelectToken("data")?.SelectToken("chat_message")?.ToString();
        BitsUsed = int.Parse(jobject.SelectToken("data")?.SelectToken("bits_used")?.ToString() ?? "");
        TotalBitsUsed = int.Parse(jobject.SelectToken("data")?.SelectToken("total_bits_used")?.ToString() ?? "");
        Context = jobject.SelectToken("data")?.SelectToken("context")?.ToString();
    }

    public string Username { get; }
    public string ChannelName { get; }
    public string UserId { get; }
    public string ChannelId { get; }
    public string Time { get; }
    public string ChatMessage { get; }
    public int BitsUsed { get; }
    public int TotalBitsUsed { get; }
    public string Context { get; }
}