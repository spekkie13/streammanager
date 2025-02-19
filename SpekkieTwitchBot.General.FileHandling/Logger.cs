using SpekkieTwitchBot.General.FileHandling.General;

namespace SpekkieTwitchBot.General.FileHandling;

public class Logger(GeneralFileWriter generalFileWriter)
{
    public void LogInfo(string text)
    {
        Console.WriteLine($"{DateTime.Now.TimeOfDay.ToString()} - INFO: {text}");
        generalFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - INFO: {text}");
    }

    public void LogWarning(string text)
    {
        Console.WriteLine($"{DateTime.Now.TimeOfDay.ToString()} - WARN: {text}");
        generalFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - WARN: {text}");
    }

    public void LogError(string text)
    {
        Console.WriteLine($"{DateTime.Now.TimeOfDay.ToString()} - ERROR: {text}");
        generalFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - ERROR: {text}");
    }
}