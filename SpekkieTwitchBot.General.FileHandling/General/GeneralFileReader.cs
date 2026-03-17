using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.General;

public class GeneralFileReader(FileReader fileReader)
{
    private const string OutputDir = "/Output/General";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public virtual string ReadAfgeleidCounter()
    {
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}Counters";
        string text = fileReader.Read(dir + "/afgeleid.txt");
        if (string.IsNullOrEmpty(text)) text = "0";
        return text;
    }
}