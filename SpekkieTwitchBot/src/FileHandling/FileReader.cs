using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling;

public class FileReader : IFileReader
{
    public string Read(string fileName)
    {
        return File.ReadAllText(fileName);
    }
}