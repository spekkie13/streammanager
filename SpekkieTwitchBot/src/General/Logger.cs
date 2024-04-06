namespace SpekkieTwitchBot.General;

public class Logger
{
    public static void LogInfo(string text)
    {
        FileHandler.WriteLogText($"INFO: {text}");
    }

    public static void LogWarning(string text)
    {
        FileHandler.WriteLogText($"WARN: {text}");
    }

    public static void LogError(string text)
    {
        FileHandler.WriteLogText($"ERROR: {text}");
    }
}