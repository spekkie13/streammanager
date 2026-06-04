#nullable disable

using Newtonsoft.Json;

namespace SpekkieClassLibrary.ClashOfClans.Ccn;

/// <summary>
/// Response of CCN's GET /players/basicinfo?player=&lt;tag&gt;. The offense/defense stat lines reuse
/// <see cref="CcnStatLine"/> (CCN's StatsBaseModel), which has the same shape as the clan-info stats.
/// </summary>
public class CcnPlayerInfo
{
    [JsonProperty("id")]
    public int? Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("accounts")]
    public List<CcnPlayer> Accounts { get; set; }

    [JsonProperty("picture_url")]
    public string PictureUrl { get; set; }

    [JsonProperty("twitter")]
    public string Twitter { get; set; }

    [JsonProperty("stats_offense")]
    public CcnStatLine StatsOffense { get; set; }

    [JsonProperty("stats_defense")]
    public CcnStatLine StatsDefense { get; set; }
}
