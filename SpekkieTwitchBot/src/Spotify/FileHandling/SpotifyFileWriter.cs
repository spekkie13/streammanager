using System.Text;
using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.Spotify.FileHandling;

public class SpotifyFileWriter
{
    private readonly FileWriter _fileWriter;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/Spotify";
    public SpotifyFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }

    public void WriteSongFile(string text)
    {
        string title = text.Split(" by ")[0];
        string titleDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        _fileWriter.Write(titleDir, title);
        
        string artist = text.Split(" by ")[1];
        string artistDir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        _fileWriter.Write(artistDir, artist);
    }
    
    public void WriteCurrentSongImage(byte[] imgBytes)
    {
        string imgPath = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}currentsong.png";
        
        try
        {
            using FileStream fileStream = new FileStream(imgPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
            File.WriteAllBytesAsync(imgPath, imgBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }
}