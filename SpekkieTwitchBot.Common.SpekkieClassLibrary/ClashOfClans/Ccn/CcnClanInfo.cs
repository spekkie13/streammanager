#nullable disable

using Newtonsoft.Json;

namespace SpekkieClassLibrary.ClashOfClans.Ccn;

public class CcnClanInfo
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("logo_url")]
    public string LogoUrl { get; set; }

    [JsonProperty("elo")]
    public int Elo { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("players")]
    public List<CcnPlayer> Players { get; set; }

    [JsonProperty("clans")]
    public List<CcnLinkedClan> Clans { get; set; }

    [JsonProperty("stats")]
    public CcnStats Stats { get; set; }
}

public class CcnPlayer
{
    [JsonProperty("tag")]
    public string Tag { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class CcnLinkedClan
{
    [JsonProperty("tag")]
    public string Tag { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class CcnStats
{
    [JsonProperty("wins")]
    public int Wins { get; set; }

    [JsonProperty("wins_by_destruction")]
    public int WinsByDestruction { get; set; }

    [JsonProperty("wins_by_duration")]
    public int WinsByDuration { get; set; }

    [JsonProperty("ties")]
    public int Ties { get; set; }

    [JsonProperty("losses")]
    public int Losses { get; set; }

    [JsonProperty("losses_by_destruction")]
    public int LossesByDestruction { get; set; }

    [JsonProperty("losses_by_duration")]
    public int LossesByDuration { get; set; }

    [JsonProperty("offense")]
    public CcnStatLine Offense { get; set; }

    [JsonProperty("defense")]
    public CcnStatLine Defense { get; set; }
}

public class CcnStatLine
{
    [JsonProperty("stats_type")]
    public string StatsType { get; set; }

    [JsonProperty("avg_stars")]
    public double AvgStars { get; set; }

    [JsonProperty("avg_perc")]
    public double AvgPerc { get; set; }

    [JsonProperty("avg_duration")]
    public double AvgDuration { get; set; }

    [JsonProperty("attacks")]
    public int Attacks { get; set; }

    [JsonProperty("triples")]
    public int Triples { get; set; }

    [JsonProperty("doubles")]
    public int Doubles { get; set; }

    [JsonProperty("singles")]
    public int Singles { get; set; }

    [JsonProperty("zeros")]
    public int Zeros { get; set; }
}