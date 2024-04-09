namespace SpekkieTwitchBot.FileHandling.General;

public class GeneralFileWriter
{
    private readonly FileWriter _fileWriter;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public GeneralFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }
    
    public void WriteLogText(string text)
    {
        string dir = BaseDir + $"{Path.DirectorySeparatorChar}Log{Path.DirectorySeparatorChar}Log.txt";
        try
        {
            using StreamWriter writer = File.AppendText(dir);
            writer.WriteLine(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"an error occured: {ex.Message}");
        }
    }
    
    public void WriteAfgeleidCounter(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Counters";
        _fileWriter.Write(dir + "/afgeleid.txt", text);
    }
}