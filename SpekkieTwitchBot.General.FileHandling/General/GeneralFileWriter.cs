using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileWriter(FileWriter fileWriter)
{
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

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

    public void WriteAfgeleidCounter(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        fileWriter.Write(dir + "/afgeleid.txt", text);
    }
}