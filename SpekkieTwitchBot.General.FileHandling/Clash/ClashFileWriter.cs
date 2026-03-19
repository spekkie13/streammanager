using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Clash;

public class ClashFileWriter : IClashFileWriter
{
    public void Write(string fileName, string data)
    {
        using FileStream fs = new(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using StreamWriter writer = new(fs);
        writer.WriteLine(data);
        writer.Flush();
    }

    public Task WriteAsync(string fileName, string data) => Task.Run(() => Write(fileName, data));

    public async Task WriteTeamNames(string file, string teamName)
    {
        await using FileStream fs = new(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using StreamWriter writer = new(fs);
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