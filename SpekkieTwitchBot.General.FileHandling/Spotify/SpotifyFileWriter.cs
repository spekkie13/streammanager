using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Spotify;

public class SpotifyFileWriter
{
    private const string OutputDir = "/Output/Spotify";

    private static readonly string BaseDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    private readonly FileWriter _fileWriter;

    public SpotifyFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }

    public void WriteSongFile(string text)
    {
        var title = text.Split(" by ")[0];
        var titleDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        _fileWriter.Write(titleDir, title);

        var artist = text.Split(" by ")[1];
        var artistDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        _fileWriter.Write(artistDir, artist);
    }

    public void WriteCurrentSongImage(byte[] imgBytes)
    {
        var imgPath = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentsong.png";

        try
        {
            using var fileStream =
                new FileStream(imgPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            fileStream.Write(imgBytes, 0, imgBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }
}