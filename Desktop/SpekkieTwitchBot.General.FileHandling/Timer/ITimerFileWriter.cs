namespace SpekkieTwitchBot.General.FileHandling.Timer;

public interface ITimerFileWriter
{
    void WriteRemainingTime(TimeSpan totalTime);
}
