using SpekkieClassLibrary.Twitch;

namespace SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

public interface ITwitchFileWriter
{
    public void WriteTwitchUserAuthFile(string text);
    Task WriteMostRecentFollowerAsync(string username, CancellationToken ct = default);
    Task WriteTotalFollowersAsync(int count, CancellationToken ct = default);
    Task WriteMostRecentSubscriberAsync(string text, CancellationToken ct = default);
    Task WriteTotalSubscribersAsync(int count, CancellationToken ct = default);
    void WriteLatestFollowerHtml(string username, int totalFollowers);
    void WriteLatestSubHtml(string subText, int totalSubs);
    void WriteSubGoalHtml(StreamGoalsConfig config);
    void WriteGoalsConfig(StreamGoalsConfig config);
}