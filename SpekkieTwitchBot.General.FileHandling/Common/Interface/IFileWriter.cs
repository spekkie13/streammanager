namespace SpekkieTwitchBot.General.FileHandling.Common.Interface;

public interface IFileWriter
{
    public void Write(string fileName, string data);
    public Task WriteAsync(string fileName, string data);
}