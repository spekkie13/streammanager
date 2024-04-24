using SpekkieClassLibrary.Twitch.Pubsub.Enums;
using SpekkieClassLibrary.Twitch.Pubsub.Types;

namespace SpekkieClassLibrary.Twitch.Pubsub.Events.Args;

public class OnPredictionArgs : EventArgs
{
    public PredictionType Type;
    public Guid Id;
    public string ChannelId;
    public DateTime? CreatedAt;
    public DateTime? LockedAt;
    public DateTime? EndedAt;
    public ICollection<Outcome> Outcomes;
    public PredictionStatus Status;
    public string Title;
    public Guid? WinningOutcomeId;
    public int PredictionTime;
}