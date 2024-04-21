using Newtonsoft.Json;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class ObsVideoSettings
{
    [JsonProperty(PropertyName = "fpsNumerator")]
    public double FpsNumerator { internal set; get; }

    [JsonProperty(PropertyName = "fpsDenominator")]
    public double FpsDenominator { internal set; get; }

    [JsonProperty(PropertyName = "baseWidth")]
    public int BaseWidth { internal set; get; }

    [JsonProperty(PropertyName = "baseHeight")]
    public int BaseHeight { internal set; get; }

    [JsonProperty(PropertyName = "outputWidth")]
    public int OutputWidth { internal set; get; }

    [JsonProperty(PropertyName = "outputHeight")]
    public int OutputHeight { internal set; get; }
}