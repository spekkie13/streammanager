using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.General.FileHandling.Timer;

public class TimerFileReader
{
    public string ReadRemainingTime()
    {
        string path = Path.Combine(BotPaths.BaseDir, "Output", "Timer", "timer.txt");

        if (!File.Exists(path)) return "00:00:00";

        string[] lines = File.ReadAllLines(path);
        return lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0])
            ? lines[0]
            : "00:00:00";
    }
}