namespace SpekkieTwitchBot.General.FileHandling.Timer;

public class TimerFileReader
{
    public string ReadRemainingTime()
    {
        string[] lines = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                          "/SpekkieTwitchBot/Output/Timer/timer.txt");
        
        return lines[0];
    }
}