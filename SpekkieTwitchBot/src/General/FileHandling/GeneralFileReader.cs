using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.General.FileHandling;

public class GeneralFileReader
{
    private readonly FileReader _fileReader;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/General";

    public GeneralFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }
    
    public string ReadAfgeleidCounter()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        string text = _fileReader.Read(dir + "/afgeleid.txt");
        if (string.IsNullOrEmpty(text)) text = "0";
        return text;
    }   
}