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

    public void WriteTeamNames(string file, string teamName)
    {
        Write(fileName: file, data: teamName);
    }
    
    public void WritePlayerName(string file, string playerName)
    {
        Write(fileName: file, data: playerName);
    }

    public void CreateTeamLogo(string file, byte[] data)
    {
        try
        {
            using var fileStream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            fileStream.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }    }
}