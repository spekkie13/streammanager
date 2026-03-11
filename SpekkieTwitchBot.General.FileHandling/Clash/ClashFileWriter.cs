using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Clash;

public class ClashFileWriter : IFileWriter
{
    public void Write(string fileName, string data)
    {
        using StreamWriter writer = new StreamWriter(fileName);
        writer.WriteLine(data);
        writer.Flush();
    }

    public Task WriteAsync(string fileName, string data) => Task.Run(() => Write(fileName, data));

    public async Task WriteTeamNames(string file, string teamName)
    {
        await using StreamWriter writer = new(file);
        await writer.WriteLineAsync(teamName);
        await writer.FlushAsync();
    }

    public void CreateTeamLogo(string file, byte[] data)
    {
        try
        {
            using FileStream fileStream = new(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            fileStream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }
}