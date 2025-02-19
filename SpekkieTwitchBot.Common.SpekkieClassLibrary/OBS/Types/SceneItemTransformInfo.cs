using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Enum;

namespace SpekkieClassLibrary.OBS.Types;

public class SceneItemTransformInfo
{
    [JsonProperty(PropertyName = "cropBottom")]
    public int CropBottom;

    [JsonProperty(PropertyName = "cropLeft")]
    public int CropLeft;

    [JsonProperty(PropertyName = "cropRight")]
    public int CropRight;

    [JsonProperty(PropertyName = "cropTop")]
    public int CropTop;

    public SceneItemTransformInfo(JObject body)
    {
        JsonConvert.PopulateObject(body.ToString(), this);
    }

    /// <summary>
    ///     Default Constructor for deserialization
    /// </summary>
    public SceneItemTransformInfo()
    {
    }

    [JsonProperty(PropertyName = "alignment")]
    public int Alignnment { set; get; }

    [JsonProperty(PropertyName = "boundsAlignment")]
    public int BoundsAlignnment { set; get; }

    [JsonProperty(PropertyName = "boundsHeight")]
    public double BoundsHeight { set; get; }

    [JsonProperty(PropertyName = "boundsWidth")]
    public double BoundsWidth { set; get; }

    [JsonProperty(PropertyName = "boundsType")]
    [JsonConverter(typeof(StringEnumConverter))]
    public SceneItemBoundsType BoundsType { set; get; }

    [JsonProperty(PropertyName = "rotation")]
    public double Rotation { set; get; }

    [JsonProperty(PropertyName = "scaleX")]
    public double ScaleX { get; set; }

    [JsonProperty(PropertyName = "scaleY")]
    public double ScaleY { get; set; }

    [JsonProperty(PropertyName = "sourceHeight")]
    public double SourceHeight { set; get; }

    [JsonProperty(PropertyName = "sourceWidth")]
    public double SourceWidth { set; get; }

    [JsonProperty(PropertyName = "height")]
    public double Height { set; get; }

    [JsonProperty(PropertyName = "width")] public double Width { set; get; }

    [JsonProperty(PropertyName = "positionX")]
    public double X { set; get; }

    [JsonProperty(PropertyName = "positionY")]
    public double Y { set; get; }
}