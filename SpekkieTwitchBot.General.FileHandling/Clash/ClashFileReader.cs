using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Clash;

public class ClashFileReader : IFileReader
{
    public string Read(string filePath)
    {
        using FileStream fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);

        using StreamReader reader = new StreamReader(fileStream);
        string content = reader.ReadToEnd();
        return content;
    }

    public Task<string> ReadAsync(string filePath) => Task.FromResult(Read(filePath));

    public string ReadPlayerTag(string filePath)
    {
        return Read(filePath);
    }

    /*public static int CountClanTags(string filePath)
    {
        int lineCount = 0;

        using StreamReader reader = new StreamReader(filePath);
        while (reader.ReadLine() != null)
            lineCount++;

        return lineCount;
    }

    public static List<string> ReadClanTags(string filePath)
    {
        List<string> clanTags = new();

        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader streamReader = new StreamReader(fileStream);
        while (streamReader.ReadLine() is { } line)
            clanTags.Add(line);

        return clanTags;
    }*/
}