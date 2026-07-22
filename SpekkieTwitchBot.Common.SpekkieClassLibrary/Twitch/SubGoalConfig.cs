using System.Text.Json.Serialization;

namespace SpekkieClassLibrary.Twitch;

// Sub-goal campaign: a running count plus an ordered ladder of milestones. The "active" goal shown to
// viewers is always the lowest tier the count hasn't reached yet, so it advances automatically.
public record SubGoalConfig(
    [property: JsonPropertyName("current")] int CurrentAmount,
    [property: JsonPropertyName("endDate")] DateOnly EndDate,
    [property: JsonPropertyName("tiers")] IReadOnlyList<SubGoalTier> Tiers
)
{
    // The next milestone still ahead of the current count, or null once every tier is reached.
    // [JsonIgnore] so this derived value never gets written back into goals.json.
    [JsonIgnore]
    public SubGoalTier? NextTier =>
        Tiers?.Where(t => t.Goal > CurrentAmount).OrderBy(t => t.Goal).FirstOrDefault();
}
