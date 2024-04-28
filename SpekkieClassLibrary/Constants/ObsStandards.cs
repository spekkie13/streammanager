using System.Globalization;

namespace SpekkieClassLibrary.Constants;

public static class ObsStandards
{
    public static float StandardMicVolume => float.Parse("0.0", CultureInfo.InvariantCulture);
    public static float StandardMusicVolume => float.Parse("-25.3", CultureInfo.InvariantCulture);
}