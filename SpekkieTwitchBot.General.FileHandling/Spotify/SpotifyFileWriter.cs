using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileWriter(FileWriter fileWriter)
{
    private const string OutputDir = "/Output/Spotify";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public void WriteSongFile(string text)
    {
        string title = text.Split(" by ")[0];
        string titleDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        fileWriter.Write(titleDir, title);

        string artist = text.Split(" by ")[1];
        string artistDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        fileWriter.Write(artistDir, artist);
    }

    public void WriteCurrentSongImage(byte[] imgBytes)
    {
        string imgPath = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentsong.png";

        try
        {
            using FileStream fileStream = new(imgPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            fileStream.Write(imgBytes, 0, imgBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }
}