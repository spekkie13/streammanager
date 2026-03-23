using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Timer;

public class TimerFileWriter(ITextFileWriter fileWriter) : ITimerFileWriter
{
    private static readonly string BaseDir = BotPaths.BaseDir;
    private const string OutputDir = "/Output/Timer";

    public void WriteRemainingTime(TimeSpan totalTime)
    {
        int hours = totalTime.Hours + 24 * totalTime.Days;
        int minutes = totalTime.Minutes;
        int seconds = totalTime.Seconds;
        string dir = $"{BaseDir}{OutputDir}{Path.DirectorySeparatorChar}timer.txt";
        string time = $"{hours.ToString().PadLeft(2,'0')}:{minutes.ToString().PadLeft(2,'0')}:{seconds.ToString().PadLeft(2,'0')}";

        try
        {
            fileWriter.Write(dir, time);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
    }
}