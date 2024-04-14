using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.General.FileHandling;

public class GeneralFileWriter
{
    private readonly FileWriter _fileWriter;
    
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/General";

    public GeneralFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
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
    
    public void WriteAfgeleidCounter(string text)
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        _fileWriter.Write(dir + "/afgeleid.txt", text);
    }
}