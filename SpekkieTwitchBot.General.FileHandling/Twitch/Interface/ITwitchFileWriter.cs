namespace SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

public interface ITwitchFileWriter
{
    public void WriteTwitchUserAuthFile(string text);
    Task WriteMostRecentFollowerAsync(string username, CancellationToken ct = default);
    Task WriteTotalFollowersAsync(int count, CancellationToken ct = default);
    Task WriteMostRecentSubscriberAsync(string text, CancellationToken ct = default);
    Task WriteTotalSubscribersAsync(int count, CancellationToken ct = default);
    void WriteLatestFollowerHtml(string username);
    void WriteLatestSubHtml(string subText);
    void WriteSubGoalHtml(int current, int goal, int daysRemaining);
}