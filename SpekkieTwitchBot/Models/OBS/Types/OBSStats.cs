using Newtonsoft.Json;

namespace SpekkieTwitchBot.Models.OBS.Types;

public class ObsStats
{
    [JsonProperty(PropertyName = "activeFps")]
    public double FPS { set; get; }

    [JsonProperty(PropertyName = "renderTotalFrames")]
    public long RenderTotalFrames { set; get; }

    [JsonProperty(PropertyName = "renderSkippedFrames")]
    public long RenderMissedFrames { set; get; }

    [JsonProperty(PropertyName = "outputTotalFrames")]
    public long OutputTotalFrames { set; get; }

    [JsonProperty(PropertyName = "outputSkippedFrames")]
    public long OutputSkippedFrames { set; get; }

    [JsonProperty(PropertyName = "averageFrameRenderTime")]
    public double AverageFrameTime { set; get; }

    [JsonProperty(PropertyName = "cpuUsage")]
    public double CpuUsage { set; get; }

    [JsonProperty(PropertyName = "memoryUsage")]
    public double MemoryUsage { set; get; }

    [JsonProperty(PropertyName = "availableDiskSpace")]
    public double FreeDiskSpace { set; get; }

    [JsonProperty(PropertyName = "webSocketSessionIncomingMessages")]
    public long SessionIncomingMessages { get; set; }

    [JsonProperty(PropertyName = "webSocketSessionOutgoingMessages")]
    public long SessionOutgoingMessages { get; set; }
}