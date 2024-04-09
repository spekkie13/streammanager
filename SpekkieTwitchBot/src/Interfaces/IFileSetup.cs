namespace SpekkieTwitchBot.Interfaces;

public interface IFileSetup
{
    public bool DirExists(string dir);
    public bool FileExists(string filePath);
    public void CreateDir(string dir);
    public void CreateFile(string filePath);
}