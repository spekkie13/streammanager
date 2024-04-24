using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;
using TwitchLib.PubSub.Extensions;
using Outcome = SpekkieClassLibrary.Twitch.Pubsub.Types.Outcome;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class PredictionEvents : MessageData
{
    public PredictionType Type { get; protected set; }
    public Guid Id { get; protected set; }
    public string ChannelId { get; protected set; }
    public DateTime? CreatedAt { get; protected set; }
    public DateTime? LockedAt { get; protected set; }
    public DateTime? EndedAt { get; protected set; }
    public ICollection<Outcome> Outcomes { get; protected set; } = new List<Outcome>();
    public PredictionStatus Status { get; protected set; }
    public string Title { get; protected set; }
    public Guid? WinningOutcomeId { get; protected set; }
    public int PredictionTime { get; protected set; }

    public PredictionEvents(string jsonStr)
    {
        JObject jobject = JObject.Parse(jsonStr);
        Type = (PredictionType)Enum.Parse(typeof(PredictionType),
            ((object)jobject.SelectToken("type")).ToString().Replace("-", ""), true);
        JToken jtoken1 = jobject.SelectToken("data.event");
        Id = Guid.Parse(((object)jtoken1.SelectToken("id")).ToString());
        ChannelId = ((object)jtoken1.SelectToken("channel_id")).ToString();
        CreatedAt = jtoken1.SelectToken("created_at").IsEmpty()
            ? new DateTime?()
            : DateTime.Parse(((object)jtoken1.SelectToken("created_at")).ToString());
        EndedAt = jtoken1.SelectToken("ended_at").IsEmpty()
            ? new DateTime?()
            : DateTime.Parse(((object)jtoken1.SelectToken("ended_at")).ToString());
        LockedAt = jtoken1.SelectToken("locked_at").IsEmpty()
            ? new DateTime?()
            : DateTime.Parse(((object)jtoken1.SelectToken("locked_at")).ToString());
        Status = (PredictionStatus)Enum.Parse(typeof(PredictionStatus),
            ((object)jtoken1.SelectToken("status")).ToString().Replace("_", ""), true);
        Title = ((object)jtoken1.SelectToken("title")).ToString();
        WinningOutcomeId = jtoken1.SelectToken("winning_outcome_id").IsEmpty()
            ? new Guid?()
            : Guid.Parse(((object)jtoken1.SelectToken("winning_outcome_id")).ToString());
        PredictionTime = int.Parse(((object)jtoken1.SelectToken("prediction_window_seconds")).ToString());
        JEnumerable<JToken> jenumerable = jtoken1.SelectToken("outcomes").Children();
        foreach (JToken jtoken2 in jenumerable)
        {
            Outcome outcome = new Outcome
            {
                Id = Guid.Parse(((object)jtoken2.SelectToken("id")).ToString()),
                Color = ((object)jtoken2.SelectToken("color")).ToString(),
                Title = ((object)jtoken2.SelectToken("title")).ToString(),
                TotalPoints = long.Parse(((object)jtoken2.SelectToken("total_points")).ToString()),
                TotalUsers = long.Parse(((object)jtoken2.SelectToken("total_users")).ToString())
            };
            jenumerable = jtoken2.SelectToken("top_predictors").Children();
            foreach (JToken jtoken3 in jenumerable)
                outcome.TopPredictors.Add(new Outcome.Predictor
                {
                    DisplayName = ((object)jtoken3.SelectToken("user_display_name")).ToString(),
                    Points = int.Parse(((object)jtoken3.SelectToken("points")).ToString()),
                    UserId = ((object)jtoken3.SelectToken("user_id")).ToString()
                });
            Outcomes.Add(outcome);
        }
    }
}