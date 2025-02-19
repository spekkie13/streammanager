using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileReader
{
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileReader _fileReader;

    public GeneralFileReader(FileReader fileReader)
    {
        _fileReader = fileReader;
    }

    public string ReadAfgeleidCounter()
    {
        var dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        var text = _fileReader.Read(dir + "/afgeleid.txt");
        if (string.IsNullOrEmpty(text)) text = "0";
        return text;
    }
}