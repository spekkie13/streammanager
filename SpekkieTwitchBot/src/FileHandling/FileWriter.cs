using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling;

public class FileWriter : IFileWriter
{
    public void Write(string fileName, string data)
    {
        File.WriteAllText(path: fileName, data);
    }
}