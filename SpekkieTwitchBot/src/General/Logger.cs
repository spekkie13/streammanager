using SpekkieTwitchBot.General.FileHandling;

namespace SpekkieTwitchBot.General;

public class Logger
{
    private readonly GeneralFileWriter _generalFileWriter;
    
    public Logger(GeneralFileWriter generalFileWriter)
    {
        _generalFileWriter = generalFileWriter;
    }
    
    public void LogInfo(string text)
    {
        _generalFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - INFO: {text}");
    }

    public void LogWarning(string text)
    {
        _generalFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - WARN: {text}");
    }

    public void LogError(string text)
    {
        _generalFileWriter.WriteLogText($"{DateTime.Now.TimeOfDay.ToString()} - ERROR: {text}");
    }
}