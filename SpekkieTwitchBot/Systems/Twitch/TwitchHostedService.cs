using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;

namespace SpekkieTwitchBot.Systems.Twitch;

public class TwitchHostedService : IHostedService
{
    private readonly ITwitchChat _Chat;
    private readonly ITwitchEvents _Events;
    private readonly TwitchEventRouter _Router;
    
    public TwitchHostedService(
        ITwitchChat chat,
        ITwitchEvents events,
        TwitchEventRouter router) 
    {
        _Chat = chat;
        _Events = events;
        _Router = router;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _Router.Wire(cancellationToken);
        
        await _Chat.ConnectAsync(cancellationToken);
        await _Events.ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _Router.Unwire();
        
        await _Chat.DisconnectAsync(cancellationToken);
        await _Events.DisconnectAsync(cancellationToken);
    }
}