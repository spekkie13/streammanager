using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class Whisper : MessageData
{
    public Whisper(string jsonStr)
    {
        JObject jobject = JObject.Parse(jsonStr);
        Type = jobject.SelectToken("type")?.ToString();
        Data = jobject.SelectToken("data")?.ToString();
        switch (Type)
        {
            case "whisper_received":
                if (jobject.SelectToken("data_object") == null) return;
                TypeEnum = WhisperType.WhisperReceived;
                DataObjectWhisperReceived = new DataObjWhisperReceived(jobject.SelectToken("data_object"));
                break;
            case "thread":
                TypeEnum = WhisperType.Thread;
                DataObjectThread = new DataObjThread(jobject.SelectToken("data_object"));
                break;
            default:
                TypeEnum = WhisperType.Unknown;
                break;
        }
    }

    private string? Type { get; }
    public WhisperType TypeEnum { get; }
    public string? Data { get; }
    public DataObjWhisperReceived? DataObjectWhisperReceived { get; }
    public DataObjThread? DataObjectThread { get; }

    public class DataObjThread(JToken? json)
    {
        public string? Id { get; } = json?.SelectToken("id")?.ToString();

        public long LastRead { get; } = long.Parse(json?.SelectToken("last_read")?.ToString() ?? "");

        public bool Archived { get; } = bool.Parse(json?.SelectToken("archived")?.ToString() ?? "");

        public bool Muted { get; } = bool.Parse(json?.SelectToken("muted")?.ToString() ?? "");

        public SpamInfoObj? SpamInfo { get; } = new(json?.SelectToken("spam_info"));

        public class SpamInfoObj(JToken? json)
        {
            public string? Likelihood { get; } = json?.SelectToken("likelihood")?.ToString();

            public long LastMarkedNotSpam { get; } = long.Parse(json?.SelectToken("last_marked_not_spam")?.ToString() ?? "");
        }
    }

    public class DataObjWhisperReceived(JToken? json)
    {
        public string? Id { get; protected set; } = json?.SelectToken("id")?.ToString();

        public string? ThreadId { get; protected set; } = json?.SelectToken("thread_id")?.ToString();

        public string? Body { get; protected set; } = json?.SelectToken("body")?.ToString();

        public long SentTs { get; protected set; } = long.Parse(json?.SelectToken("sent_ts")?.ToString() ?? "");

        public string? FromId { get; protected set; } = json?.SelectToken("from_id")?.ToString();

        public TagsObj? Tags { get; protected set; } = new(json?.SelectToken("tags"));

        public RecipientObj Recipient { get; protected set; } = new(json?.SelectToken("recipient"));

        public string? Nonce { get; protected set; } = json?.SelectToken("nonce")?.ToString();

        public class TagsObj
        {
            private readonly List<Badge> _badges = [];
            private readonly List<EmoteObj> _emotes = [];

            public TagsObj(JToken? json)
            {
                Login = json?.SelectToken("login")?.ToString();
                DisplayName = json?.SelectToken("login")?.ToString();
                Color = json?.SelectToken("color")?.ToString();
                UserType = json?.SelectToken("user_type")?.ToString();
                foreach (JToken json1 in json?.SelectToken("emotes")!)
                    _emotes.Add(new EmoteObj(json1));
                foreach (JToken json2 in json.SelectToken("badges")!)
                    _badges.Add(new Badge(json2));
            }

            public string? Login { get; protected set; }

            public string? DisplayName { get; protected set; }

            public string? Color { get; protected set; }

            public string? UserType { get; protected set; }

            private class EmoteObj(JToken json)
            {
                public string? Id { get; protected set; } = json.SelectToken("emote_id")?.ToString();

                public int Start { get; protected set; } = int.Parse(json.SelectToken("start")?.ToString() ?? "");

                public int End { get; protected set; } = int.Parse(json.SelectToken("end")?.ToString() ?? "");
            }
        }

        public class RecipientObj(JToken? json)
        {
            public string? Id { get; protected set; } = json?.SelectToken("id")?.ToString();

            public string? Username { get; protected set; } = json?.SelectToken("username")?.ToString();

            public string? DisplayName { get; protected set; } = json?.SelectToken("display_name")?.ToString();

            public string? Color { get; protected set; } = json?.SelectToken("color")?.ToString();

            public string? UserType { get; protected set; } = json?.SelectToken("user_type")?.ToString();
        }

        private class Badge(JToken json)
        {
            public string? Id { get; protected set; } = json.SelectToken("id")?.ToString();

            public string? Version { get; protected set; } = json.SelectToken("version")?.ToString();
        }
    }
}