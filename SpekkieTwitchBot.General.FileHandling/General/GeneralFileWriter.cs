using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileWriter
{
    private readonly ITextFileWriter _FileWriter;
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir = BotPaths.BaseDir;

    public GeneralFileWriter(ITextFileWriter fileWriter)
    {
        _FileWriter = fileWriter;
    }
    
    public void WriteLogText(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Log{Path.DirectorySeparatorChar}Log.txt";
        try
        {
            using FileStream fs = new(dir, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter writer = new(fs);
            writer.WriteLine(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"an error occured writing log text: {ex.Message}");
        }
    }

    public virtual void WriteAfgeleidCounter(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        _FileWriter.Write(dir + "/afgeleid.txt", text);
    }
}