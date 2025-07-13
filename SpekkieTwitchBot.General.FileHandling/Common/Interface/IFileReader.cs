namespace SpekkieTwitchBot.General.FileHandling.Common.Interface;

public interface IFileReader
{
    public string Read(string fileName);
    public Task<string> ReadAsync(string fileName);
}