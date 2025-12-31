using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Http;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

public class TwitchInfoJob : BackgroundService
{
    private readonly ITwitchFileWriter _TwitchFileWriter;
    private readonly CustomTwitchHttpClient _CustomTwitchHttpClient;

    public TwitchInfoJob(ITwitchFileWriter twitchFileWriter, CustomTwitchHttpClient customTwitchHttpClient)
    {
        _TwitchFileWriter = twitchFileWriter;
        _CustomTwitchHttpClient = customTwitchHttpClient;
    }
    
    private async Task UpdateTwitchInfo(CancellationToken ct)
    {
        string recentFollower = await _CustomTwitchHttpClient.GetLatestFollower(ct);
        await _TwitchFileWriter.WriteMostRecentFollowerAsync(recentFollower, ct);
        
        int totalFollowers = await _CustomTwitchHttpClient.GetFollowerCount(ct);
        await _TwitchFileWriter.WriteTotalFollowersAsync(totalFollowers, ct);
        
        string recentSubscriber = await _CustomTwitchHttpClient.GetLatestSubscriber(ct);
        await _TwitchFileWriter.WriteMostRecentSubscriberAsync(recentSubscriber, ct);
        
        int totalSubscribers = await _CustomTwitchHttpClient.GetSubscriberCount(ct);
        await _TwitchFileWriter.WriteTotalSubscribersAsync(totalSubscribers, ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            await UpdateTwitchInfo(stoppingToken);
        }
    }
}