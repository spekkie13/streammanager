using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;

namespace SpekkieClassLibrary.Twitch.Pubsub.Types;

public class SubMessage : MessageData
{
    public string Message { get; }
    public List<Emote> Emotes { get; } = new();

    public SubMessage(JToken json)
    {
        Message = ((object)json.SelectToken("message"))?.ToString();
        foreach (JToken json1 in (IEnumerable<JToken>)json.SelectToken("emotes"))
            Emotes.Add(new Emote(json1));
    }

    public class Emote
    {
        public int Start { get; }
        public int End { get; }
        public string Id { get; }

        public Emote(JToken json)
        {
            Start = int.Parse(((object)json.SelectToken("start")).ToString());
            End = int.Parse(((object)json.SelectToken("end")).ToString());
            Id = ((object)json.SelectToken("id")).ToString();
        }
    }
}