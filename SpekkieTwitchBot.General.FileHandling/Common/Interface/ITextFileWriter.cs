namespace SpekkieTwitchBot.General.FileHandling.Common.Interface;

public interface ITextFileWriter
{
    void Write(string fileName, string data);
    Task WriteAsync(string fileName, string data);
}
