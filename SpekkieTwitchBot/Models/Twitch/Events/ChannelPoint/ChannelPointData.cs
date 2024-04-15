namespace SpekkieTwitchBot.Models.Twitch.Events.ChannelPoint;

public class ChannelPointData
{
    public string broadcaster_name { get; set; }
    public string broadcaster_id { get; set; }
    public string id { get; set; }
    public string background_color { get; set; }
    public bool is_enabled { get; set; }
    public int cost { get; set; }
    public string title { get; set; }
    public string prompt { get; set; }
    public bool is_user_input_required { get; set; }
    public MaxSetting max_per_stream_setting { get; set; }
    public MaxSetting max_per_user_per_stream_setting { get; set; }
    public CooldownSetting global_cooldown_setting { get; set; }
    public bool is_paused { get; set; }
    public bool is_in_stock { get; set; }
    public ImageData default_image { get; set; }
    public bool should_redemptions_skip_request_queue { get; set; }
    public object redemptions_redeemed_current_stream { get; set; }
    public object cooldown_expires_at { get; set; }
}