using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Common;

public class FileReader : IFileReader
{
    public string Read(string fileName)
    {
        using var fileStream = new FileStream(
            fileName,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);

        using var reader = new StreamReader(fileStream);
        var content = reader.ReadToEnd();
        return content;
    }
    
    public static string ReadStatic(string fileName)
    {
        using var fileStream = new FileStream(
            fileName,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite);

        using var reader = new StreamReader(fileStream);
        var content = reader.ReadToEnd();
        return content;
    }
}