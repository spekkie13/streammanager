using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Common;

public class FileSetup : IFileSetup
{
    public bool DirExists(string dir)
    {
        return Directory.Exists(dir);
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public void CreateDir(string dir)
    {
        Directory.CreateDirectory(dir);
    }

    public void CreateFile(string filePath)
    {
        try
        {
            using FileStream fs = new(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}