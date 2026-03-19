using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Common;

public class FileWriter : ITextFileWriter
{
    public void Write(string fileName, string data)
    {
        using FileStream fs = new(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter writer = new(fs);
        writer.WriteLine(data);
        writer.Flush();
    }

    public async Task WriteAsync(string fileName, string data)
    {
        await using FileStream fs = new(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using StreamWriter writer = new(fs);
        await writer.WriteLineAsync(data);
        await writer.FlushAsync();
    }
}