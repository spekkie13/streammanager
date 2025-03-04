using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Common;

public class FileReader : IFileReader
{
    public string Read(string fileName)
    {
        using FileStream fileStream = new (
            fileName,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);

        using StreamReader reader = new (fileStream);
        string content = reader.ReadToEnd();
        return content;
    }
}