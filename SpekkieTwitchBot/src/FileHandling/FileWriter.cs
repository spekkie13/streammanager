using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling;

public class FileWriter : IFileWriter
{
    public void Write(string fileName, string data)
    {
        using StreamWriter writer = new StreamWriter(fileName);
        writer.WriteLine(data);
        writer.Flush();
    }
}