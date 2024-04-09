using SpekkieTwitchBot.FileHandling.General;

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
        _generalFileWriter.WriteLogText($"INFO: {text}");
    }

    public void LogWarning(string text)
    {
        _generalFileWriter.WriteLogText($"WARN: {text}");
    }

    public void LogError(string text)
    {
        _generalFileWriter.WriteLogText($"ERROR: {text}");
    }
}