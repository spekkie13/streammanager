namespace SpekkieTwitchBot.General.FileHandling.Twitch.Interface;

public interface ITwitchFileWriter
{
    public void WriteTwitchUserAuthFile(string text);
    public Task WriteMostRecentFollowerFileAsync(string text);
    public Task WriteTotalFollowersFileAsync(int totalFollowers);
    public Task WriteMostRecentSubscriberFileAsync(string text);
    public Task WriteTotalSubscribersFileAsync(int totalSubscribers);
}