using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace SpekkieTwitchBot.Web;

public class TwitchWebsocketService : IHostedService
{
    private readonly IConfiguration _Configuration;
    private readonly ILogger<TwitchWebsocketService> _Logger;
    private readonly EventSubWebsocketClient _EventSubWebsocketClient;

    public TwitchWebsocketService(IConfiguration configuration, ILogger<TwitchWebsocketService> logger,
        EventSubWebsocketClient eventSubWebsocketClient)
    {
        _Configuration = configuration;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _EventSubWebsocketClient =
            eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
        _EventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
        _EventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
        _EventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;

        _EventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;
        _EventSubWebsocketClient.ChannelFollow += OnChannelFollow;
    }
    
    private void OnErrorOccurred(object? sender, ErrorOccuredArgs e)
    {
        _Logger.LogError($"Websocket {_EventSubWebsocketClient.SessionId} - Error occurred!");
    }

    private void OnChannelFollow(object? sender, ChannelFollowArgs e)
    {
        var eventData = e.Notification.Payload.Event;
        _Logger.LogInformation($"{eventData.UserName} followed {eventData.BroadcasterUserName} at {eventData.FollowedAt}");
    }
    
    private void OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    {
        _Logger.LogInformation($"Websocket {_EventSubWebsocketClient.SessionId} connected!");

        if (!e.IsRequestedReconnect)
        {
            // subscribe to topics
        }
    }

    private async void OnWebsocketDisconnected(object? sender, EventArgs e)
    {
        _Logger.LogError($"Websocket {_EventSubWebsocketClient.SessionId} disconnected!");

        // Don't do this in production. You should implement a better reconnect strategy
        while (!await _EventSubWebsocketClient.ReconnectAsync())
        {
            _Logger.LogError("Websocket reconnect failed!");
            await Task.Delay(1000);
        }
    }

    private void OnWebsocketReconnected(object? sender, EventArgs e)
    {
        _Logger.LogWarning($"Websocket {_EventSubWebsocketClient.SessionId} reconnected");
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _EventSubWebsocketClient.ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _EventSubWebsocketClient.DisconnectAsync();
    }
}