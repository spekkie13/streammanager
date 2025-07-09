namespace SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;

public class OnRaidUpdateArgs : EventArgs
{
    public string? ChannelId;
    public Guid? Id;
    public string? TargetChannelId;
    public DateTime AnnounceTime;
    public DateTime RaidTime;
    public int RemainingDurationSeconds;
    public int ViewerCount;
}