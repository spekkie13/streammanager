using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling;

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
        File.Create(filePath);
    }
}