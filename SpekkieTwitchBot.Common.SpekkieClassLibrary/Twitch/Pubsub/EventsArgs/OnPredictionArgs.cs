using SpekkieClassLibrary.Twitch.Pubsub.Enums;
using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class PredictionArgs : EventArgs
{
    public string? ChannelId;
    public DateTime? CreatedAt;
    public DateTime? EndedAt;
    public Guid? Id;
    public DateTime? LockedAt;
    public ICollection<Outcome>? Outcomes;
    public int? PredictionTime;
    public PredictionStatus? Status;
    public string? Title;
    public PredictionType? Type;
    public Guid? WinningOutcomeId;
}