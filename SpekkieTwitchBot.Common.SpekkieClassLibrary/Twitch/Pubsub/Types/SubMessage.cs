#nullable disable
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class SubMessage : MessageData
{
    public SubMessage(JToken json)
    {
        Message = json.SelectToken("message")?.ToString();
        foreach (JToken json1 in json.SelectToken("emotes")!)
            Emotes.Add(new Emote(json1));
    }

    public string Message { get; }
    public List<Emote> Emotes { get; } = new();

    public class Emote(JToken json)
    {
        public int Start { get; } = int.Parse(json.SelectToken("start")?.ToString()!);
        public int End { get; } = int.Parse(json.SelectToken("end")?.ToString()!);
        public string Id { get; } = json.SelectToken("id")?.ToString()!;
    }
}