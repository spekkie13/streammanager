using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Twitch.Pubsub.Abstract;
using SpekkieClassLibrary.Twitch.Pubsub.Enums;
using TwitchLib.PubSub.Extensions;
using Outcome = SpekkieClassLibrary.Twitch.Pubsub.Types.Outcome;

#nullable disable
namespace SpekkieClassLibrary.Twitch.Pubsub.EventData;

public class PredictionEvents : MessageData
{
    public PredictionEvents(string jsonStr)
    {
        JObject jobject = JObject.Parse(jsonStr);
        Type = (PredictionType)Enum.Parse(typeof(PredictionType),
            jobject.SelectToken("type")!.ToString().Replace("-", ""), true);
        JToken jtoken1 = jobject.SelectToken("data.event");
        if (jtoken1 == null) return;
        Id = Guid.Parse(jtoken1.SelectToken("id")?.ToString() ?? "");
        ChannelId = jtoken1.SelectToken("channel_id")?.ToString();
        CreatedAt = jtoken1.SelectToken("created_at").IsEmpty()
            ? null
            : DateTime.Parse(jtoken1.SelectToken("created_at")?.ToString() ?? "");
        EndedAt = jtoken1.SelectToken("ended_at").IsEmpty()
            ? null
            : DateTime.Parse(jtoken1.SelectToken("ended_at")?.ToString() ?? "");
        LockedAt = jtoken1.SelectToken("locked_at").IsEmpty()
            ? null
            : DateTime.Parse(jtoken1.SelectToken("locked_at")?.ToString() ?? "");
        Status = (PredictionStatus)Enum.Parse(typeof(PredictionStatus),
            jtoken1.SelectToken("status")!.ToString().Replace("_", ""), true);
        Title = jtoken1.SelectToken("title")?.ToString();
        WinningOutcomeId = jtoken1.SelectToken("winning_outcome_id").IsEmpty()
            ? null
            : Guid.Parse(jtoken1.SelectToken("winning_outcome_id")?.ToString() ?? "");
        PredictionTime = int.Parse(jtoken1.SelectToken("prediction_window_seconds")?.ToString() ?? "");
        JToken outcomes = jtoken1.SelectToken("outcomes");
        if (outcomes == null) return;
        JEnumerable<JToken> jenumerable = outcomes.Children();
        foreach (JToken jtoken2 in jenumerable)
        {
            string id = jtoken2.SelectToken("id")?.ToString() ?? "";
            string color = jtoken2.SelectToken("color")?.ToString() ?? "";
            string title = jtoken2.SelectToken("title")?.ToString() ?? "";
            string totalPoints = jtoken2.SelectToken("total_points")?.ToString() ?? "";
            string totalUsers = jtoken2.SelectToken("total_users")?.ToString() ?? "";

            Outcome outcome = new Outcome
            {
                Id = Guid.Parse(id),
                Color = color,
                Title = title,
                TotalPoints = long.Parse(totalPoints),
                TotalUsers = long.Parse(totalUsers)
            };
            JToken topPredictors = jtoken1.SelectToken("top_predictors");
            if (topPredictors == null) return;
            jenumerable = topPredictors.Children();
            foreach (JToken jtoken3 in jenumerable)
            {
                string userDisplayName = jtoken3.SelectToken("user_display_name")?.ToString() ?? "";
                string points = jtoken3.SelectToken("points")?.ToString() ?? "";
                string userId = jtoken3.SelectToken("user_id")?.ToString() ?? "";

                outcome.TopPredictors.Add(new Outcome.Predictor
                {
                    DisplayName = userDisplayName,
                    Points = int.Parse(points),
                    UserId = userId
                });
            }

            Outcomes.Add(outcome);
        }
    }

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
}