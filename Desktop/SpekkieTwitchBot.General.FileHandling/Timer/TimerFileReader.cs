using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Timer;

public class TimerFileReader
{
    public string ReadRemainingTime()
    {
        string[] lines = File.ReadAllLines(
            Path.Combine(BotPaths.BaseDir, "Output", "Timer", "timer.txt"));
        
        return lines[0];
    }
}