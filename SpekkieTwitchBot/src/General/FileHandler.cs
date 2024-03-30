using System.Text;

namespace SpekkieTwitchBot.General;

public static class FileHandler
{
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    static FileHandler()
    {
        SetupSongFiles();
        SetupTimerFile();
        SetupCounterFiles();
    }

    private static void SetupCounterFiles()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Counters";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(dir + "/afgeleid.txt"))
            File.Create(dir + "/afgeleid.txt");
    }
    
    private static void SetupTimerFile()
    {
        string dir = BaseDir + $"{Path.DirectorySeparatorChar}Timer";
        if(!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if(!File.Exists(dir + "/timer.txt"))
            File.Create(dir + "/timer.txt");
    }

    private static void SetupSongFiles()
    {
        string titleDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        string artistDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        string pictureDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentSong.png";

        if (!File.Exists(titleDir))
            File.Create(titleDir);

        if (!File.Exists(artistDir))
            File.Create(artistDir);        
        
        if (!File.Exists(pictureDir))
            File.Create(pictureDir);
    }
    
    public static void WriteRemainingTime(TimeSpan totalTime)
    {
        int hours = totalTime.Hours + 24 * totalTime.Days;
        int minutes = totalTime.Minutes;
        int seconds = totalTime.Seconds;
        string dir = BaseDir + $"{Path.DirectorySeparatorChar}Timer{Path.DirectorySeparatorChar}timer.txt";
        string time = $"{hours.ToString().PadLeft(2,'0')}:{minutes.ToString().PadLeft(2,'0')}:{seconds.ToString().PadLeft(2,'0')}";

        try
        {
            using FileStream fileStream = new FileStream(dir, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
            writer.WriteLine(time);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }

    public static string ReadTwitchAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch.json";
        string jsonData = File.ReadAllText(dir);

        return jsonData;
    }

    public static void WriteTwitchAuthFile(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Twitch.json";
        File.WriteAllText(dir, text);
    }
    public static string ReadSpotifyAuthFile()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Settings{Path.DirectorySeparatorChar}Spotify.json";
        string jsonData = File.ReadAllText(dir);

        return jsonData;
    }

    public static void WriteSongFile(string text)
    {
        string title = text.Split(" by ")[0];
        string artist = text.Split(" by ")[1];
        string titleDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentTitle.txt";
        string artistDir = $"{BaseDir}{Path.DirectorySeparatorChar}currentArtist.txt";
        File.WriteAllText(titleDir, title);
        File.WriteAllText(artistDir, artist);
    }
    
    public static void WriteCurrentSongImage(byte[] imgBytes)
    {
        string imgPath = $"{BaseDir}{Path.DirectorySeparatorChar}currentsong.png";
        
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

    public static void WriteAfgeleidCounter(string text)
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Counters";
        File.WriteAllText(dir + "/afgeleid.txt", text);
    }
    
    public static string ReadAfgeleidCounter()
    {
        string dir = $"{BaseDir}{Path.DirectorySeparatorChar}Counters";
        string text = File.ReadAllText(dir + "/afgeleid.txt");
        if (string.IsNullOrEmpty(text)) text = "0";
        return text;
    }
}