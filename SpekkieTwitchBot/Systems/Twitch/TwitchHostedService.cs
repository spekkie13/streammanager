using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Application.Routing;

namespace SpekkieTwitchBot.Systems.Twitch;

public class TwitchHostedService(
    ITwitchChat chat,
    ITwitchEvents events,
    TwitchEventRouter router
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        router.Wire();
        
        await chat.ConnectAsync(cancellationToken);
        await events.ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        router.Unwire();
        
        await chat.DisconnectAsync(cancellationToken);
        await events.DisconnectAsync(cancellationToken);
    }
}