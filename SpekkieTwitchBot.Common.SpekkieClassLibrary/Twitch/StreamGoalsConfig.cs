using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Twitch;

public record StreamGoalsConfig(
    [property: JsonPropertyName("followerGoal")] int FollowerGoal,
    [property: JsonPropertyName("subGoal")] SubGoalConfig SubGoal
);
