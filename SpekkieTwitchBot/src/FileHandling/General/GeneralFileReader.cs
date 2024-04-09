namespace SpekkieTwitchBot.FileHandling.General;

public class GeneralFileReader
{
    private readonly FileReader _fileReader;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    
    public GeneralFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }
    
    public string ReadAfgeleidCounter()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Counters";
        string text = _fileReader.Read(dir + "/afgeleid.txt");
        if (string.IsNullOrEmpty(text)) text = "0";
        return text;
    }   
}