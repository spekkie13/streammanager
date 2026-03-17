using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileWriter
{
    private readonly FileWriter _FileWriter;
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public GeneralFileWriter(FileWriter fileWriter)
    {
        _FileWriter = fileWriter;
    }
    
    public void WriteLogText(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Log{Path.DirectorySeparatorChar}Log.txt";
        try
        {
            using StreamWriter writer = File.AppendText(dir);
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