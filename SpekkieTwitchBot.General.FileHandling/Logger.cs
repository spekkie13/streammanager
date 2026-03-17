using SpekkieTwitchBot.General.FileHandling.General;

namespace SpekkieTwitchBot.General.FileHandling;

public class Logger
{
    private readonly GeneralFileWriter _GeneralFileWriter;
    
    public Logger(GeneralFileWriter generalFileWriter)
    {
        _GeneralFileWriter = generalFileWriter;
    }
    
    public virtual void LogInfo(string text)
    {
        Console.WriteLine($"{DateTime.Now.TimeOfDay.ToString()} - INFO: {text}");
        _GeneralFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - INFO: {text}");
    }

    public virtual void LogWarning(string text)
    {
        Console.WriteLine($"{DateTime.Now.TimeOfDay.ToString()} - WARN: {text}");
        _GeneralFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - WARN: {text}");
    }

    public virtual void LogError(string text)
    {
        Console.WriteLine($"{DateTime.Now.TimeOfDay.ToString()} - ERROR: {text}");
        _GeneralFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - ERROR: {text}");
    }
}