using System.Text;
using SpekkieTwitchBot.Interfaces;

namespace SpekkieTwitchBot.FileHandling.Timer;

public class TimerFileWriter
{
    private readonly FileWriter _fileWriter;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public TimerFileWriter(FileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }
    
    public void WriteRemainingTime(TimeSpan totalTime)
    {
        int hours = totalTime.Hours + 24 * totalTime.Days;
        int minutes = totalTime.Minutes;
        int seconds = totalTime.Seconds;
        string dir = BaseDir + $"{Path.DirectorySeparatorChar}Timer{Path.DirectorySeparatorChar}timer.txt";
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