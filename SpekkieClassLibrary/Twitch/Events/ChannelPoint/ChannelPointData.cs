#nullable disable
namespace SpekkieClassLibrary.Twitch.Events.ChannelPoint;

public class ChannelPointData
{
    public string BroadcasterName { get; set; }
    public string BroadcasterId { get; set; }
    public string Id { get; set; }
    public string BackgroundColor { get; set; }
    public bool IsEnabled { get; set; }
    public int Cost { get; set; }
    public string Title { get; set; }
    public string Prompt { get; set; }
    public bool IsUserInputRequired { get; set; }
    public MaxSetting MaxPerStreamSetting { get; set; }
    public MaxSetting MaxPerUserPerStreamSetting { get; set; }
    public CooldownSetting GlobalCooldownSetting { get; set; }
    public bool IsPaused { get; set; }
    public bool IsInStock { get; set; }
    public ImageData DefaultImage { get; set; }
    public bool ShouldRedemptionsSkipRequestQueue { get; set; }
    public object RedemptionsRedeemedCurrentStream { get; set; }
    public object CooldownExpiresAt { get; set; }
}