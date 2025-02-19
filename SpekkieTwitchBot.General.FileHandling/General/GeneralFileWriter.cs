using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileWriter
{
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileWriter _fileWriter;

    public GeneralFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }

    public void WriteLogText(string text)
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Log{Path.DirectorySeparatorChar}Log.txt";
        try
        {
            using var writer = File.AppendText(dir);
            writer.WriteLine(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"an error occured writing log text: {ex.Message}");
        }
    }

    public void WriteAfgeleidCounter(string text)
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        _fileWriter.Write(dir + "/afgeleid.txt", text);
    }
}