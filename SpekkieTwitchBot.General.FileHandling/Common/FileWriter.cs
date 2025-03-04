using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Common;

public class FileWriter : IFileWriter
{
    public void Write(string fileName, string data)
    {
        using StreamWriter writer = new (fileName);
        writer.WriteLine(data);
        writer.Flush();
    }
}