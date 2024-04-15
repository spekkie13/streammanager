using SpekkieTwitchBot.FileHandling;

namespace SpekkieTwitchBot.Timer.FileHandling;

public class TimerFileWriter
{
    private readonly FileWriter _fileWriter;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private const string OutputDir = "/Output/Timer";
    public TimerFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }
    
    public void WriteRemainingTime(TimeSpan totalTime)
    {
        int hours = totalTime.Hours + 24 * totalTime.Days;
        int minutes = totalTime.Minutes;
        int seconds = totalTime.Seconds;
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}timer.txt";
        string time = $"{hours.ToString().PadLeft(2,'0')}:{minutes.ToString().PadLeft(2,'0')}:{seconds.ToString().PadLeft(2,'0')}";

        try
        {
            _fileWriter.Write(dir, time);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }
}