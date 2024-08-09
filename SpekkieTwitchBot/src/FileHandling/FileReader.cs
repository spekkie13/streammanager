using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling;

public class FileReader : IFileReader
{
    public string Read(string fileName)
    {
        using FileStream fileStream = new FileStream(
            fileName,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);

        using StreamReader reader = new StreamReader(fileStream);
        string content = reader.ReadToEnd();
        return content;    }
}